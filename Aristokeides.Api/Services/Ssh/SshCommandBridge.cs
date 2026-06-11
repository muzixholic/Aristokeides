using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Services;
using Aristokeides.Api.Services.Webhook;
using Microsoft.DevTunnels.Ssh;
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

    public async Task RunGitCommandAsync(SshChannel channel, string commandName, string repoPath, SshSessionState state)
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
                await channel.CloseAsync(1, CancellationToken.None);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while starting git process.");
            await channel.CloseAsync(1, CancellationToken.None);
            return;
        }

        var channelStream = new SshStream(channel);
        var cts = new CancellationTokenSource();

        // Stdin 전송
        var stdinTask = Task.Run(async () =>
        {
            try
            {
                await channelStream.CopyToAsync(process.StandardInput.BaseStream, cts.Token);
            }
            catch { }
            finally
            {
                try { process.StandardInput.Close(); } catch { }
            }
        });

        // Stdout/Stderr 전송
        var stdoutTask = CopyStreamToChannelAsync(process.StandardOutput.BaseStream, channelStream, cts.Token);
        var stderrTask = CopyStreamToChannelAsync(process.StandardError.BaseStream, channelStream, cts.Token);

        try
        {
            await process.WaitForExitAsync();
            cts.Cancel(); // Stop stdin copying if it hasn't stopped
            
            await Task.WhenAll(stdoutTask, stderrTask);
            
            channel.CloseAsync((uint)process.ExitCode, CancellationToken.None).GetAwaiter().GetResult();

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

                                            // 웹훅 push 이벤트 트리거
                                            try
                                            {
                                                var webhookService = scope.ServiceProvider.GetRequiredService<WebhookService>();
                                                var commitsList = new List<object>();
                                                try
                                                {
                                                    if (oldOid != "0000000000000000000000000000000000000000")
                                                    {
                                                        var filter = new LibGit2Sharp.CommitFilter
                                                        {
                                                            IncludeReachableFrom = newOid,
                                                            ExcludeReachableFrom = oldOid
                                                        };
                                                        foreach (var c in repo.Commits.QueryBy(filter))
                                                        {
                                                            commitsList.Add(new
                                                            {
                                                                id = c.Sha,
                                                                message = c.MessageShort,
                                                                author = c.Author.Name
                                                            });
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var commit = repo.Lookup(newOid) as LibGit2Sharp.Commit;
                                                        if (commit != null)
                                                        {
                                                            commitsList.Add(new
                                                            {
                                                                id = commit.Sha,
                                                                message = commit.MessageShort,
                                                                author = commit.Author.Name
                                                            });
                                                        }
                                                    }
                                                }
                                                catch (Exception commitEx)
                                                {
                                                    _logger.LogWarning(commitEx, "Failed to build commit list for SSH webhook payload.");
                                                }

                                                var pushPayload = new
                                                {
                                                    @event = "push",
                                                    repository = new
                                                    {
                                                        id = repository.Id,
                                                        name = repository.Name,
                                                        owner = repository.Owner?.Username ?? "unknown"
                                                    },
                                                    sender = new
                                                    {
                                                        id = state.UserId,
                                                        username = state.Username
                                                    },
                                                    data = new
                                                    {
                                                        @ref = r.CanonicalName,
                                                        before = oldOid,
                                                        after = newOid,
                                                        commits = commitsList
                                                    }
                                                };

                                                await webhookService.TriggerWebhookAsync(repository.Id, "push", pushPayload);
                                            }
                                            catch (Exception webEx)
                                            {
                                                _logger.LogError(webEx, "Failed to trigger SSH push webhook.");
                                            }
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
            await channel.CloseAsync(1, CancellationToken.None);
        }
        finally
        {
            try { process.Dispose(); } catch { }
        }
    }

    private async Task CopyStreamToChannelAsync(Stream source, Stream destination, CancellationToken cancellationToken)
    {
        try
        {
            await source.CopyToAsync(destination, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception) { }
    }
}
