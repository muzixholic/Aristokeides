using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.DevTunnels.Ssh;
using Microsoft.DevTunnels.Ssh.Algorithms;
using Microsoft.DevTunnels.Ssh.Events;
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
    public static string LastAuthFailedUsername { get; set; } = string.Empty;
    public static int ServiceRegisteredCount { get; set; }
    public static int KeysExchangedCount { get; set; }
    public static System.Collections.Generic.List<string> DebugLogs { get; set; } = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SshServerBackgroundService> _logger;
    private readonly int _port;

    public SshServerBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<SshServerBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _port = configuration.GetValue<int>("Ssh:Port", 2222);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string keyPath = Path.Combine(Directory.GetCurrentDirectory(), "host.key");
        IKeyPair hostKeyPair;

        if (File.Exists(keyPath))
        {
            string pemKey = await File.ReadAllTextAsync(keyPath, stoppingToken);
            using var ecdsa = System.Security.Cryptography.ECDsa.Create();
            ecdsa.ImportFromPem(pemKey);
            // Need to create a new ECDsa with the same parameters so it owns the handle
            var parameters = ecdsa.ExportParameters(true);
            var clonedEcdsa = System.Security.Cryptography.ECDsa.Create(parameters);
            hostKeyPair = new Microsoft.DevTunnels.Ssh.Algorithms.ECDsa.KeyPair(clonedEcdsa);
        }
        else
        {
            using var ecdsa = System.Security.Cryptography.ECDsa.Create(ECCurve.NamedCurves.nistP256);
            string pemKey = ecdsa.ExportECPrivateKeyPem();
            await File.WriteAllTextAsync(keyPath, pemKey, stoppingToken);
            var parameters = ecdsa.ExportParameters(true);
            var clonedEcdsa = System.Security.Cryptography.ECDsa.Create(parameters);
            hostKeyPair = new Microsoft.DevTunnels.Ssh.Algorithms.ECDsa.KeyPair(clonedEcdsa);
        }

        var config = new SshSessionConfiguration();
        config.AddService(typeof(Microsoft.DevTunnels.Ssh.Services.SshService)); // Wait, maybe AuthenticationService is built-in or handled automatically?
        
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        _logger.LogInformation("SSH Server started on port {Port}", _port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, config, hostKeyPair, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SSH Server is stopping.");
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, SshSessionConfiguration config, IKeyPair hostKeyPair, CancellationToken cancellationToken)
    {
        string clientIp = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        DebugLogs.Add($"[Server] Remote IP: {clientIp}");
        _logger.LogInformation("SSH connection accepted: {Ip}", clientIp);

        try
        {
            using var stream = client.GetStream();
            var session = new SshServerSession(config, new System.Diagnostics.TraceSource("SshServer"));
            session.Credentials = new SshServerCredentials(new[] { hostKeyPair });
            
            session.Authenticating += (sender, e) => OnAuthenticating(sender, e, clientIp);
            session.ChannelOpening += OnChannelOpening;

            await session.ConnectAsync(stream, cancellationToken);
            await session.CloseAsync(SshDisconnectReason.ByApplication, "Server shutdown");
        }
        catch (Exception ex)
        {
            LastException = ex;
            DebugLogs.Add($"[Server-Exception] {ex}");
            _logger.LogError(ex, "SSH Session Exception");
        }
        finally
        {
            client.Dispose();
            DebugLogs.Add($"[Session] Disconnected");
        }
    }

    private void OnAuthenticating(object? sender, SshAuthenticatingEventArgs e, string clientIp)
    {
        _logger.LogWarning("UserAuth invoked. Username: {Username}, KeyAlgorithm: {KeyAlgorithm}, HasKey: {HasKey}", 
            e.Username, e.PublicKey?.KeyAlgorithmName, e.PublicKey != null);

        e.AuthenticationTask = AuthenticateAsync(e, clientIp);
    }

    private async Task<ClaimsPrincipal> AuthenticateAsync(SshAuthenticatingEventArgs e, string clientIp)
    {
        if (e.Username != "git")
        {
            LastAuthFailureReason = $"Username '{e.Username}' is not 'git'";
            LastAuthFailedUsername = e.Username ?? "";
            _logger.LogWarning("UserAuth failed: Username '{Username}' is not 'git'", e.Username);
            await LogAuthAttempt(clientIp, null, e.Username, false, LastAuthFailureReason, null);
            throw new Exception("Authentication failed");
        }

        if (e.PublicKey == null)
        {
            LastAuthFailureReason = "Public key is required for authentication.";
            _logger.LogWarning("UserAuth failed: No public key provided for user '{Username}'", e.Username);
            await LogAuthAttempt(clientIp, null, e.Username, false, LastAuthFailureReason, null);
            throw new Exception("Authentication failed");
        }

        // Generate fingerprint
        // e.PublicKey is IKeyPair, we need to export it to compute SHA256 fingerprint
        byte[] publicKeyBytes = e.PublicKey.GetPublicKeyBytes().ToArray();
        string fingerprint = "SHA256:" + Convert.ToBase64String(SHA256.HashData(publicKeyBytes)).TrimEnd('=');
        
        _logger.LogInformation("Attempting SSH auth with fingerprint: {Fingerprint}", fingerprint);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sshKey = await dbContext.SshKeys
            .Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Fingerprint == fingerprint);

        if (sshKey != null && sshKey.User != null)
        {
            _logger.LogInformation("SSH Key matched user: {Username}", sshKey.User.Username);
            
            await LogAuthAttempt(clientIp, fingerprint, e.Username, true, null, e.PublicKey.KeyAlgorithmName);
            
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, sshKey.UserId.ToString()),
                new Claim(ClaimTypes.Name, sshKey.User.Username)
            };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "SshAuth"));
        }
        else
        {
            LastAuthFailureReason = $"SSH Key not found in DB for fingerprint: {fingerprint}.";
            _logger.LogWarning("SSH Key not found in DB for fingerprint: {Fingerprint}", fingerprint);
            await LogAuthAttempt(clientIp, fingerprint, e.Username, false, LastAuthFailureReason, e.PublicKey.KeyAlgorithmName);
            throw new Exception("Authentication failed");
        }
    }

    private async Task LogAuthAttempt(string clientIp, string? fingerprint, string? username, bool isSuccess, string? failureReason, string? keyType)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            dbContext.SshAuthLogs.Add(new SshAuthLog
            {
                ClientIp = clientIp,
                KeyFingerprint = fingerprint,
                Username = username,
                IsSuccess = isSuccess,
                FailureReason = failureReason,
                KeyType = keyType
            });
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log SSH authentication attempt.");
        }
    }

    private void OnChannelOpening(object? sender, SshChannelOpeningEventArgs e)
    {
        if (e.Request.ChannelType != "session")
        {
            e.FailureReason = Microsoft.DevTunnels.Ssh.Messages.SshChannelOpenFailureReason.UnknownChannelType;
            return;
        }

        var session = (SshServerSession)sender!;
        var principal = session.Principal;
        if (principal == null)
        {
            e.FailureReason = Microsoft.DevTunnels.Ssh.Messages.SshChannelOpenFailureReason.AdministrativelyProhibited;
            return;
        }

        e.Channel.Request += async (s, req) => await OnChannelRequestAsync(e.Channel, req, principal);
    }

    private async Task OnChannelRequestAsync(SshChannel channel, SshRequestEventArgs<Microsoft.DevTunnels.Ssh.Messages.ChannelRequestMessage> e, ClaimsPrincipal principal)
    {
        if (e.Request.RequestType != "exec")
        {
            e.IsAuthorized = false;
            return;
        }
        
        e.IsAuthorized = true;

        if (e.Request is Microsoft.DevTunnels.Ssh.Messages.CommandRequestMessage execReq)
        {
            string cmd = execReq.Command ?? string.Empty;
            
            int userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            string username = principal.FindFirst(ClaimTypes.Name)?.Value ?? "";
            
            var state = new SshSessionState { UserId = userId, Username = username };

            if (string.IsNullOrEmpty(cmd))
            {
                byte[] welcomeMessage = System.Text.Encoding.UTF8.GetBytes(
                    $"Hi {state.Username}! You've successfully authenticated, but Aristokeides does not provide shell access.\r\n"
                );
                await channel.SendAsync(welcomeMessage, CancellationToken.None);
                await channel.CloseAsync(0, CancellationToken.None);
                return;
            }

            // Whitelist check
            if (!cmd.StartsWith("git-upload-pack") && !cmd.StartsWith("git-receive-pack"))
            {
                byte[] errorMessage = System.Text.Encoding.UTF8.GetBytes("Interactive shell is not allowed.\r\n");
                await channel.SendAsync(errorMessage, CancellationToken.None);
                await channel.CloseAsync(1, CancellationToken.None);
                return;
            }

            // Parse command and argument
            var parts = cmd.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                await channel.CloseAsync(1, CancellationToken.None);
                return;
            }

            string commandName = parts[0];
            string repoPath = parts[1].Trim('\'', '"');

            // Directory Traversal check
            if (repoPath.Contains(".."))
            {
                await channel.CloseAsync(1, CancellationToken.None);
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
                await channel.CloseAsync(1, CancellationToken.None);
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
                await channel.SendAsync(notFound, CancellationToken.None);
                await channel.CloseAsync(1, CancellationToken.None);
                return;
            }

            // Permission check
            bool isWriteAction = commandName.Equals("git-receive-pack", StringComparison.OrdinalIgnoreCase);

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
                await channel.SendAsync(denied, CancellationToken.None);
                await channel.CloseAsync(1, CancellationToken.None);
                return;
            }

            // Pass to bridge
            var bridge = scope.ServiceProvider.GetRequiredService<SshCommandBridge>();
            await bridge.RunGitCommandAsync(channel, commandName, repoPath, state);
        }
    }
}
