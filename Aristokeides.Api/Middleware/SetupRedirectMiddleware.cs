using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Aristokeides.Api.Middleware;

public class SetupRedirectMiddleware
{
    private readonly RequestDelegate _next;

    public SetupRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var isInstalled = configuration.GetValue<bool>("IsInstalled");
        var path = context.Request.Path.Value?.ToLowerInvariant();

        // Allow static assets, blazor framework files, and setup page
        var isExempt = path != null && (
            path.StartsWith("/setup") || 
            path.StartsWith("/_framework") || 
            path.StartsWith("/_blazor") || 
            path.StartsWith("/css") || 
            path.StartsWith("/js") ||
            path.StartsWith("/lib") ||
            path.StartsWith("/_content")
        );

        if (!isInstalled && !isExempt)
        {
            context.Response.Redirect("/setup");
            return;
        }

        await _next(context);
    }
}
