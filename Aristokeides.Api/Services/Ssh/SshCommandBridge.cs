using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Services;
using FxSsh.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aristokeides.Api.Services.Ssh;

public class SshCommandBridge
{
    private readonly ILogger<SshCommandBridge> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SshSignatureVerificationService _signatureVerificationService;

    public SshCommandBridge(
        ILogger<SshCommandBridge> logger,
        IServiceProvider serviceProvider,
        SshSignatureVerificationService signatureVerificationService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _signatureVerificationService = signatureVerificationService;
    }

    public async Task RunGitCommandAsync(SessionChannel channel, string commandName, string repoPath, SshSessionState state)
    {
        _logger.LogInformation("Running {Command} for repository {RepoPath} by user {Username}", commandName, repoPath, state.Username);

        string physicalRepoPath = Path.Combine(Directory.GetCurrentDirectory(), "Repositories", repoPath + ".git");

        // git-receive-pack(Push) 인 경우, 실행 전의 브랜치 Head OID 목록 수집
        var beforeRefs = new Dictionary<string, string>();
        bool isPush = commandName.Equals("git-receive-pack", StringComparison.OrdinalIgnoreCase);
        
        if (isPush && Directory.Exists(physicalRepoPath))
        {
            try
            {
                using var repo = new LibGit2Sharp.Repository(physicalRepoPath);
                foreach (var r in repo.Refs)
                {
                    if (r.CanonicalName.StartsWith("refs/heads/"))
                    {
                        beforeRefs[r.CanonicalName] = r.TargetIdentifier;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to inspect repository references before push.");
            }
        }

        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"{commandName.Substring(4)} \"{physicalRepoPath}\"", // e.g. upload-pack "..."
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process? process;
        try
        {
            process = Process.Start(psi);
            if (process == null)
            {
                _logger.LogError("Failed to start git process.");
                channel.SendClose(1);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while starting git process.");
            channel.SendClose(1);
            return;
        }

        channel.DataReceived += (sender, args) =>
        {
            try
            {
                process.StandardInput.BaseStream.Write(args);
                process.StandardInput.BaseStream.Flush();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write to git process standard input.");
            }
        };
        
        channel.EofReceived += (sender, args) =>
        {
            try
            {
                process.StandardInput.Close();
            }
            catch { }
        };
        
        channel.CloseReceived += (sender, args) =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch { }
        };

        try
        {
            var stdoutTask = CopyStreamToChannelAsync(process.StandardOutput.BaseStream, channel);
            var stderrTask = CopyStreamToChannelExtendedAsync(process.StandardError.BaseStream, channel);

            await process.WaitForExitAsync();
            
            try { process.StandardInput.Close(); } catch { }

            await Task.WhenAll(stdoutTask, stderrTask);
            
            channel.SendClose((uint)process.ExitCode);

            // Push가 성공(ExitCode == 0)적으로 끝났을 때 신규 커밋들에 대해 서명 검증 수행
            if (isPush && process.ExitCode == 0 && Directory.Exists(physicalRepoPath))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<Microsoft.EntityFrameworkCore.DbContextOptions<AppDbContext>>();
                        using var dbContext = new AppDbContext(db);

                        var pathParts = repoPath.Split('/');
                        if (pathParts.Length == 2)
                        {
                            string ownerName = pathParts[0];
                            string repoName = pathParts[1];

                            var repository = await dbContext.Repositories
                                .Include(r => r.Owner)
                                .FirstOrDefaultAsync(r => r.Owner != null && r.Owner.Username == ownerName && r.Name == repoName);

                            if (repository != null)
                            {
                                using var repo = new LibGit2Sharp.Repository(physicalRepoPath);
                                foreach (var r in repo.Refs)
                                {
                                    if (r.CanonicalName.StartsWith("refs/heads/"))
                                    {
                                        string newOid = r.TargetIdentifier;
                                        beforeRefs.TryGetValue(r.CanonicalName, out string? oldOid);
                                        oldOid ??= "0000000000000000000000000000000000000000";

                                        if (oldOid != newOid)
                                        {
                                            _logger.LogInformation("SSH Push detected: ref {Ref} changed from {Old} to {New}. Verifying signatures & post-push processing...", r.CanonicalName, oldOid, newOid);
                                            await _signatureVerificationService.VerifyNewCommitsAsync(physicalRepoPath, oldOid, newOid, repository.Id);

                                            // PullRequestService 후처리 호출
                                            var prService = scope.ServiceProvider.GetRequiredService<PullRequestService>();
                                            var branchName = r.CanonicalName.Substring("refs/heads/".Length);
                                            await prService.OnBranchPushedAsync(repository.Id, branchName, oldOid, newOid);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error executing post-SSH push signature verification.");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while piping git process.");
            channel.SendClose(1);
        }
        finally
        {
            try { process.Dispose(); } catch { }
        }
    }

    private async Task CopyStreamToChannelAsync(Stream source, SessionChannel channel)
    {
        byte[] buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            byte[] data = new byte[bytesRead];
            Array.Copy(buffer, data, bytesRead);
            channel.SendData(data);
        }
    }

    private async Task CopyStreamToChannelExtendedAsync(Stream source, SessionChannel channel)
    {
        byte[] buffer = new byte[4096];
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            byte[] data = new byte[bytesRead];
            Array.Copy(buffer, data, bytesRead);
            // Since FxSsh SessionChannel might not have SendExtendedData out of the box easily,
            // we will fallback to standard SendData for stderr messages to client.
            channel.SendData(data);
        }
    }
}
