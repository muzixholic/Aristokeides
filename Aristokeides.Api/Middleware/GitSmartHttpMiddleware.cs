using System.Diagnostics;
using System.Security.Claims;
using Aristokeides.Api.Data;
using Aristokeides.Api.Services;
using Aristokeides.Api.Services.Ssh;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Aristokeides.Api.Services.Webhook;


namespace Aristokeides.Api.Middleware;

public class GitSmartHttpMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GitSmartHttpMiddleware> _logger;

    public GitSmartHttpMiddleware(RequestDelegate next, ILogger<GitSmartHttpMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db, SshSignatureVerificationService sigService, IServiceProvider serviceProvider)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            await _next(context);
            return;
        }

        // Match /{username}/{repo.name}.git/{*path}
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 3 || !segments[1].EndsWith(".git"))
        {
            await _next(context);
            return;
        }

        // LFS API 요청 우회 (/{username}/{repo.name}.git/info/lfs/...)
        if (segments.Length >= 4 && 
            segments[2].Equals("info", StringComparison.OrdinalIgnoreCase) && 
            segments[3].Equals("lfs", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var ownerOrOrgName = segments[0];
        var repoNameWithGit = segments[1];
        var repoName = repoNameWithGit.Substring(0, repoNameWithGit.Length - 4);
        var gitPathInfo = "/" + string.Join("/", segments.Skip(1)); // keeping repo.git/...

        // Basic Auth required
        var authResult = await context.AuthenticateAsync("Basic");
        if (!authResult.Succeeded)
        {
            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Aristokeides Git\"";
            context.Response.StatusCode = 401;
            return;
        }

        var userIdString = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdString, out var userId))
        {
            context.Response.StatusCode = 401;
            return;
        }

        // Verify repo exists and user has access
        var repo = await db.Repositories
            .Include(r => r.Owner)
            .Include(r => r.Organization)
            .FirstOrDefaultAsync(r => 
                (r.Owner != null && r.Owner.Username == ownerOrOrgName && r.Name == repoName) ||
                (r.Organization != null && r.Organization.Name == ownerOrOrgName && r.Name == repoName));

        if (repo == null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        // Determine if this is a write/push operation
        bool isWriteAction = false;
        if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            if (segments.Length >= 3 && segments[2].Equals("git-receive-pack", StringComparison.OrdinalIgnoreCase))
            {
                isWriteAction = true;
            }
        }
        else if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            if (context.Request.QueryString.HasValue && context.Request.QueryString.Value!.Contains("service=git-receive-pack"))
            {
                isWriteAction = true;
            }
        }

        // Determine maximum access level
        string? maxAccess = null;
        if (repo.OwnerId == userId)
        {
            maxAccess = "Admin";
        }
        else if (repo.OrganizationId.HasValue)
        {
            bool isOrgOwner = await db.OrganizationMembers.AnyAsync(om => 
                om.OrganizationId == repo.OrganizationId.Value && 
                om.UserId == userId && 
                om.Role == "Owner");

            if (isOrgOwner)
            {
                maxAccess = "Admin";
            }
            else
            {
                var teamIds = await db.TeamMembers
                    .Where(tm => tm.UserId == userId && tm.Team.OrganizationId == repo.OrganizationId.Value)
                    .Select(tm => tm.TeamId)
                    .ToListAsync();

                var permissions = await db.RepositoryPermissions
                    .Where(rp => rp.RepositoryId == repo.Id && 
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

        // Apply access control rules
        if (isWriteAction)
        {
            // Push requires Admin or Write
            if (maxAccess != "Admin" && maxAccess != "Write")
            {
                context.Response.StatusCode = 403;
                return;
            }
        }
        else
        {
            // Pull/Clone/Fetch: Private repositories require at least Read
            if (repo.IsPrivate)
            {
                if (maxAccess != "Admin" && maxAccess != "Write" && maxAccess != "Read")
                {
                    context.Response.StatusCode = 403;
                    return;
                }
            }
        }

        var basePath = Path.GetFullPath("GitRepos");
        var gitProjectRoot = Path.Combine(basePath, ownerOrOrgName);
        var physicalRepoPath = Path.Combine(gitProjectRoot, repoNameWithGit);

        // HTTP Push인지 확인하고, 실행 전 refs 수집
        bool isPush = context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) && 
                      segments.Length >= 3 && 
                      segments[2].Equals("git-receive-pack", StringComparison.OrdinalIgnoreCase);

        var beforeRefs = new Dictionary<string, string>();
        if (isPush && Directory.Exists(physicalRepoPath))
        {
            try
            {
                using var gitRepo = new LibGit2Sharp.Repository(physicalRepoPath);
                foreach (var r in gitRepo.Refs)
                {
                    if (r.CanonicalName.StartsWith("refs/heads/"))
                    {
                        beforeRefs[r.CanonicalName] = r.TargetIdentifier;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to inspect repository references before HTTP push.");
            }
        }

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "http-backend",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = gitProjectRoot
        };

        processStartInfo.EnvironmentVariables["GIT_PROJECT_ROOT"] = gitProjectRoot;
        processStartInfo.EnvironmentVariables["PATH_INFO"] = gitPathInfo;
        processStartInfo.EnvironmentVariables["GIT_HTTP_EXPORT_ALL"] = "1";
        processStartInfo.EnvironmentVariables["REMOTE_USER"] = ownerOrOrgName;
        processStartInfo.EnvironmentVariables["REQUEST_METHOD"] = context.Request.Method;
        processStartInfo.EnvironmentVariables["CONTENT_TYPE"] = context.Request.ContentType ?? "";
        
        if (context.Request.QueryString.HasValue)
        {
            processStartInfo.EnvironmentVariables["QUERY_STRING"] = context.Request.QueryString.Value!.TrimStart('?');
        }

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var copyInputTask = context.Request.Body.CopyToAsync(process.StandardInput.BaseStream);
        var readOutputTask = ProcessGitOutputAsync(process.StandardOutput.BaseStream, context);
        
        await Task.WhenAll(copyInputTask, readOutputTask);
        process.StandardInput.Close();
        
        await process.WaitForExitAsync();

        // HTTP Push 완료 후 신규 커밋들에 대해 서명 검증 수행 및 브랜치 푸시 후처리 실행
        if (isPush && process.ExitCode == 0 && Directory.Exists(physicalRepoPath))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var gitRepo = new LibGit2Sharp.Repository(physicalRepoPath);
                    foreach (var r in gitRepo.Refs)
                    {
                        if (r.CanonicalName.StartsWith("refs/heads/"))
                        {
                            string newOid = r.TargetIdentifier;
                            beforeRefs.TryGetValue(r.CanonicalName, out string? oldOid);
                            oldOid ??= "0000000000000000000000000000000000000000";

                            if (oldOid != newOid)
                            {
                                _logger.LogInformation("HTTP Push detected: ref {Ref} changed from {Old} to {New}. Verifying signatures & post-push processing...", r.CanonicalName, oldOid, newOid);
                                await sigService.VerifyNewCommitsAsync(physicalRepoPath, oldOid, newOid, repo.Id);

                                // PullRequestService 후처리 호출
                                using var scope = serviceProvider.CreateScope();
                                var prService = scope.ServiceProvider.GetRequiredService<PullRequestService>();
                                var branchName = r.CanonicalName.Substring("refs/heads/".Length);
                                await prService.OnBranchPushedAsync(repo.Id, branchName, oldOid, newOid);

                                // 웹훅 push 이벤트 트리거
                                try
                                {
                                    var webhookService = scope.ServiceProvider.GetRequiredService<WebhookService>();
                                    var scopedDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                                    var senderUser = await scopedDb.Users.FindAsync(userId);

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
                                            foreach (var c in gitRepo.Commits.QueryBy(filter))
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
                                            var commit = gitRepo.Lookup(newOid) as LibGit2Sharp.Commit;
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
                                        _logger.LogWarning(commitEx, "Failed to build commit list for webhook payload.");
                                    }

                                    var pushPayload = new
                                    {
                                        @event = "push",
                                        repository = new
                                        {
                                            id = repo.Id,
                                            name = repo.Name,
                                            owner = repo.Owner?.Username ?? repo.Organization?.Name ?? "unknown"
                                        },
                                        sender = new
                                        {
                                            id = senderUser?.Id ?? userId,
                                            username = senderUser?.Username ?? "unknown-user"
                                        },
                                        data = new
                                        {
                                            @ref = r.CanonicalName,
                                            before = oldOid,
                                            after = newOid,
                                            commits = commitsList
                                        }
                                    };

                                    await webhookService.TriggerWebhookAsync(repo.Id, "push", pushPayload);
                                }
                                catch (Exception webEx)
                                {
                                    _logger.LogError(webEx, "Failed to trigger push webhook.");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing HTTP push post-processing (signatures & branch pushed flow).");
                }
            });
        }
    }

    private async Task ProcessGitOutputAsync(Stream gitStream, HttpContext context)
    {
        using var reader = new StreamReader(gitStream, leaveOpen: true);
        string? line;
        
        // Read headers
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex == -1) continue;

            var key = line.Substring(0, separatorIndex).Trim();
            var value = line.Substring(separatorIndex + 1).Trim();

            if (key.Equals("Status", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(value.Split(' ')[0], out var statusCode))
                {
                    context.Response.StatusCode = statusCode;
                }
            }
            else
            {
                context.Response.Headers[key] = value;
            }
        }

        // Write body
        await gitStream.CopyToAsync(context.Response.Body);
    }
}
