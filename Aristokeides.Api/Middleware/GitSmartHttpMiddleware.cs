using System.Diagnostics;
using System.Security.Claims;
using Aristokeides.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

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

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
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

        var username = segments[0];
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
            .FirstOrDefaultAsync(r => r.Owner!.Username == username && r.Name == repoName);

        if (repo == null)
        {
            context.Response.StatusCode = 404;
            return;
        }

        // Currently, basic check: only owner can access
        if (repo.OwnerId != userId)
        {
            context.Response.StatusCode = 403;
            return;
        }

        var basePath = Path.GetFullPath("GitRepos");
        var gitProjectRoot = Path.Combine(basePath, username);

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
        processStartInfo.EnvironmentVariables["REMOTE_USER"] = username;
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
