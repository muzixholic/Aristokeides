using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FxSsh;
using FxSsh.Services;
using Aristokeides.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aristokeides.Api.Services.Ssh;

public class SshServerBackgroundService : BackgroundService
{
    public static Exception? LastException { get; set; }
    public static string LastAuthFailureReason { get; set; } = string.Empty;
    public static int ServiceRegisteredCount { get; set; }
    public static int KeysExchangedCount { get; set; }
    public static System.Collections.Generic.List<string> DebugLogs { get; set; } = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SshServerBackgroundService> _logger;
    private readonly int _port;
    private SshServer? _server;
    private readonly ConcurrentDictionary<Session, SshSessionState> _sessions = new();

    public SshServerBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SshServerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _port = configuration.GetValue<int>("Ssh:Port", 2222);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string keyPath = Path.Combine(Directory.GetCurrentDirectory(), "host.key");
        string pemKey;
        if (File.Exists(keyPath))
        {
            pemKey = File.ReadAllText(keyPath);
        }
        else
        {
            using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            pemKey = ecdsa.ExportECPrivateKeyPem();
            File.WriteAllText(keyPath, pemKey);
        }

        _server = new SshServer(new StartingInfo(System.Net.IPAddress.Any, _port, "SSH-2.0-FxSsh"));
        _server.ExceptionRasied += (sender, ex) =>
        {
            LastException = ex;
            DebugLogs.Add($"[Server-Exception] {ex}");
            _logger.LogError(ex, "FxSsh Server Exception raised");
        };
        _server.AddHostKey("ecdsa-sha2-nistp256", pemKey);

        _server.ConnectionAccepted += (sender, session) =>
        {
            DebugLogs.Add($"[Server] ConnectionAccepted");
            // 리플렉션을 통해 private 소켓 및 IP 정보 획득
            string ip = "unknown";
            try
            {
                var socketField = typeof(Session).GetField("_socket", BindingFlags.NonPublic | BindingFlags.Instance);
                var socket = socketField?.GetValue(session) as System.Net.Sockets.Socket;
                ip = socket?.RemoteEndPoint?.ToString() ?? "unknown";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get remote endpoint from session.");
            }

            DebugLogs.Add($"[Server] Remote IP: {ip}");
            _logger.LogInformation("SSH connection accepted: {Ip}", ip);
            
            session.Disconnected += (s, args) =>
            {
                DebugLogs.Add($"[Session] Disconnected");
            };

            session.KeysExchanged += (s, args) =>
            {
                KeysExchangedCount++;
                DebugLogs.Add($"[Session] KeysExchanged");
            };

            session.ServiceRegistered += (s, service) =>
            {
                ServiceRegisteredCount++;
                DebugLogs.Add($"[Session] ServiceRegistered: {service.GetType().FullName}");
                _logger.LogInformation("Service registered: {ServiceType}", service.GetType().FullName);
                if (service is UserauthService authService)
                {
                    authService.Userauth += (authSender, authArgs) => OnUserAuth(session, authArgs);
                }
                else if (service is ConnectionService connectionService)
                {
                    connectionService.CommandOpened += async (cmdSender, cmdArgs) => await OnCommandOpened(session, cmdArgs);
                }
            };
        };

        _server.Start();
        _logger.LogInformation("FxSsh server started on port {Port}", _port);

        var tcs = new TaskCompletionSource();
        stoppingToken.Register(() =>
        {
            try
            {
                _server.Stop();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while stopping FxSsh server.");
            }
            tcs.SetResult();
        });

        return tcs.Task;
    }

    private void OnUserAuth(Session session, UserauthArgs e)
    {
        _logger.LogWarning("UserAuth invoked. Username: {Username}, KeyAlgorithm: {KeyAlgorithm}, Fingerprint: {Fingerprint}, HasKey: {HasKey}", 
            e.Username, e.KeyAlgorithm, e.Fingerprint, e.Key != null);

        // User는 항상 'git' 이어야 함
        if (e.Username != "git")
        {
            LastAuthFailureReason = $"Username '{e.Username}' is not 'git'";
            _logger.LogWarning("UserAuth failed: Username '{Username}' is not 'git'", e.Username);
            e.Result = false;
            return;
        }

        if (e.Key == null)
        {
            LastAuthFailureReason = "Public key is required for authentication.";
            _logger.LogWarning("UserAuth failed: No public key provided for user '{Username}'", e.Username);
            e.Result = false;
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 클라이언트가 제시한 공개키 데이터로부터 SHA-256 지문 연산
        string fingerprint = "SHA256:" + Convert.ToBase64String(SHA256.HashData(e.Key)).TrimEnd('=');
        _logger.LogInformation("Attempting SSH auth with fingerprint: {Fingerprint}", fingerprint);

        // DB에서 키 검색
        var sshKey = dbContext.SshKeys
            .Include(k => k.User)
            .FirstOrDefault(k => k.Fingerprint == fingerprint);

        if (sshKey != null && sshKey.User != null)
        {
            _logger.LogInformation("SSH Key matched user: {Username}", sshKey.User.Username);
            e.Result = true;
            _sessions[session] = new SshSessionState
            {
                UserId = sshKey.UserId,
                Username = sshKey.User.Username
            };
        }
        else
        {
            LastAuthFailureReason = $"SSH Key not found in DB for fingerprint: {fingerprint}. DB keys: {string.Join(", ", dbContext.SshKeys.Select(k => k.Fingerprint))}";
            _logger.LogWarning("SSH Key not found in DB for fingerprint: {Fingerprint}", fingerprint);
            e.Result = false;
        }
    }

    private async Task OnCommandOpened(Session session, CommandRequestedArgs e)
    {
        if (!_sessions.TryGetValue(session, out var state))
        {
            e.Channel.SendClose(1);
            return;
        }

        string cmd = e.CommandText?.Trim() ?? string.Empty;

        // 1. 'ssh -T' 명령(단순 진단 접속) 처리
        if (string.IsNullOrEmpty(cmd))
        {
            byte[] welcomeMessage = System.Text.Encoding.UTF8.GetBytes(
                $"Hi {state.Username}! You've successfully authenticated, but Aristokeides does not provide shell access.\r\n"
            );
            e.Channel.SendData(welcomeMessage);
            e.Channel.SendClose(0);
            return;
        }

        // Whitelist check
        if (!cmd.StartsWith("git-upload-pack") && !cmd.StartsWith("git-receive-pack"))
        {
            byte[] errorMessage = System.Text.Encoding.UTF8.GetBytes("Interactive shell is not allowed.\r\n");
            e.Channel.SendData(errorMessage);
            e.Channel.SendClose(1);
            return;
        }

        // Parse command and argument
        var parts = cmd.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            e.Channel.SendClose(1);
            return;
        }

        string commandName = parts[0];
        string repoPath = parts[1].Trim('\'', '"');

        // Directory Traversal check
        if (repoPath.Contains(".."))
        {
            e.Channel.SendClose(1);
            return;
        }

        // Normalize path
        repoPath = repoPath.TrimStart('/');
        if (repoPath.EndsWith(".git"))
        {
            repoPath = repoPath.Substring(0, repoPath.Length - 4);
        }

        var pathParts = repoPath.Split('/');
        if (pathParts.Length != 2)
        {
            e.Channel.SendClose(1);
            return;
        }

        string ownerName = pathParts[0];
        string repoName = pathParts[1];

        // Validation against database
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var repository = await dbContext.Repositories
            .Include(r => r.Owner)
            .Include(r => r.Organization)
            .FirstOrDefaultAsync(r => 
                (r.Owner != null && r.Owner.Username == ownerName && r.Name == repoName) ||
                (r.Organization != null && r.Organization.Name == ownerName && r.Name == repoName));

        if (repository == null)
        {
            byte[] notFound = System.Text.Encoding.UTF8.GetBytes("Repository not found\r\n");
            e.Channel.SendData(notFound);
            e.Channel.SendClose(1);
            return;
        }

        // Permission check
        bool isWriteAction = commandName.Equals("git-receive-pack", StringComparison.OrdinalIgnoreCase);
        int userId = state.UserId;

        string? maxAccess = null;
        if (repository.OwnerId == userId)
        {
            maxAccess = "Admin";
        }
        else if (repository.OrganizationId.HasValue)
        {
            bool isOrgOwner = await dbContext.OrganizationMembers.AnyAsync(om => 
                om.OrganizationId == repository.OrganizationId.Value && 
                om.UserId == userId && 
                om.Role == "Owner");

            if (isOrgOwner)
            {
                maxAccess = "Admin";
            }
            else
            {
                var teamIds = await dbContext.TeamMembers
                    .Where(tm => tm.UserId == userId && tm.Team.OrganizationId == repository.OrganizationId.Value)
                    .Select(tm => tm.TeamId)
                    .ToListAsync();

                var permissions = await dbContext.RepositoryPermissions
                    .Where(rp => rp.RepositoryId == repository.Id && 
                                 (rp.UserId == userId || (rp.TeamId != null && teamIds.Contains(rp.TeamId.Value))))
                    .Select(rp => rp.AccessLevel)
                    .ToListAsync();

                if (permissions.Any())
                {
                    if (permissions.Contains("Admin")) maxAccess = "Admin";
                    else if (permissions.Contains("Write")) maxAccess = "Write";
                    else if (permissions.Contains("Read")) maxAccess = "Read";
                }
            }
        }

        bool hasAccess = false;
        if (isWriteAction)
        {
            hasAccess = (maxAccess == "Admin" || maxAccess == "Write");
        }
        else
        {
            if (repository.IsPrivate)
            {
                hasAccess = (maxAccess == "Admin" || maxAccess == "Write" || maxAccess == "Read");
            }
            else
            {
                hasAccess = true;
            }
        }

        if (!hasAccess)
        {
            byte[] denied = System.Text.Encoding.UTF8.GetBytes("Permission denied\r\n");
            e.Channel.SendData(denied);
            e.Channel.SendClose(1);
            return;
        }

        // Pass to bridge
        var bridge = scope.ServiceProvider.GetRequiredService<SshCommandBridge>();
        await bridge.RunGitCommandAsync(e.Channel, commandName, repoPath, state);
    }
}
