using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Aristokeides.Api.Middleware;

public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var sessionIdClaim = user.FindFirst("SessionId");
            if (sessionIdClaim != null)
            {
                var sessionId = sessionIdClaim.Value;
                var session = await db.UserSessions.FirstOrDefaultAsync(s => s.Id == sessionId);

                if (session == null || session.IsRevoked)
                {
                    // 세션이 무효화되었거나 존재하지 않음
                    await context.SignOutAsync("Cookies");
                    
                    // AJAX 요청이나 API 요청인 경우 401 반환
                    var isApiRequest = context.Request.Path.Value?.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) == true;
                    if (isApiRequest)
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    context.Response.Redirect("/login?error=session_expired");
                    return;
                }

                // LastActiveAt 업데이트 쓰로틀링 (5분 간격)
                if (DateTime.UtcNow - session.LastActiveAt >= TimeSpan.FromMinutes(5))
                {
                    session.LastActiveAt = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
        }

        await _next(context);
    }
}
