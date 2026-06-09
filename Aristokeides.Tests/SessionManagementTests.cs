using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Aristokeides.Api.Controllers;
using Aristokeides.Api.Data;
using Aristokeides.Api.Middleware;
using Aristokeides.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aristokeides.Tests;

public class SessionManagementTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly FakeAuthenticationService _authService;
    private readonly AuthController _authController;

    public SessionManagementTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"Aristokeides_Session_Test_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);

        var inMemorySettings = new Dictionary<string, string> {
            {"Jwt:Key", "super_secret_key_that_is_long_enough_for_hmac_sha256_verification_1234567890"},
            {"Jwt:Issuer", "test_issuer"},
            {"Jwt:Audience", "test_audience"}
        };
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _authService = new FakeAuthenticationService();
        var serviceProvider = new FakeServiceProvider(_authService);
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

        _authController = new AuthController(_db, _config)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<User> CreateTestUserAsync()
    {
        var user = new User
        {
            Email = "test@example.com",
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            Role = "Reader"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CookieLogin_CreatesSessionInDb_AndSetsSessionIdClaim()
    {
        // Arrange
        var user = await CreateTestUserAsync();

        // Act
        var result = await _authController.CookieLogin(user.Email, "Password123!");

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirectResult.Url);

        var dbSession = await _db.UserSessions.FirstOrDefaultAsync(s => s.UserId == user.Id);
        Assert.NotNull(dbSession);
        Assert.False(dbSession.IsRevoked);
        Assert.Equal(_authController.Request.Headers.UserAgent.ToString(), dbSession.UserAgent);

        Assert.NotNull(_authService.SignedInPrincipal);
        var sessionIdClaim = _authService.SignedInPrincipal.FindFirst("SessionId");
        Assert.NotNull(sessionIdClaim);
        Assert.Equal(dbSession.Id, sessionIdClaim.Value);
    }

    [Fact]
    public async Task Logout_RevokesSessionInDb()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            UserAgent = "TestAgent",
            IpAddress = "127.0.0.1"
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SessionId", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        _authController.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = await _authController.Logout();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirectResult.Url);

        var dbSession = await _db.UserSessions.FindAsync(sessionId);
        Assert.NotNull(dbSession);
        Assert.True(dbSession.IsRevoked);
        Assert.True(_authService.SignedOutCalled);
    }

    [Fact]
    public async Task RevokeSession_ChangesIsRevokedToTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            UserAgent = "TestAgent",
            IpAddress = "127.0.0.1"
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        _authController.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

        // Act
        var result = await _authController.RevokeSession(new RevokeSessionRequest(sessionId));

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var dbSession = await _db.UserSessions.FindAsync(sessionId);
        Assert.NotNull(dbSession);
        Assert.True(dbSession.IsRevoked);
    }

    [Fact]
    public async Task SessionValidationMiddleware_ValidSession_CallsNextAndThrottlesLastActiveAt()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var sessionId = Guid.NewGuid().ToString("N");
        var initialActiveTime = DateTime.UtcNow.AddMinutes(-6); // 5분 이상 경과된 상태로 설정
        
        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            UserAgent = "TestAgent",
            IpAddress = "127.0.0.1",
            LastActiveAt = initialActiveTime
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SessionId", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        var context = new DefaultHttpContext { RequestServices = _authController.ControllerContext.HttpContext.RequestServices };
        context.User = new ClaimsPrincipal(identity);

        var isNextCalled = false;
        var middleware = new SessionValidationMiddleware(innerHttpContext =>
        {
            isNextCalled = true;
            return Task.CompletedTask;
        });

        // Act - First Call (LastActiveAt이 업데이트되어야 함)
        await middleware.InvokeAsync(context, _db);

        // Assert
        Assert.True(isNextCalled);
        var updatedSession = await _db.UserSessions.FindAsync(sessionId);
        Assert.NotNull(updatedSession);
        Assert.True(updatedSession.LastActiveAt > initialActiveTime);
        var firstUpdatedTime = updatedSession.LastActiveAt;

        // Act - Second Call (바로 호출 시 5분 이내이므로 업데이트되지 않아야 함)
        isNextCalled = false;
        await middleware.InvokeAsync(context, _db);

        // Assert
        Assert.True(isNextCalled);
        var secondSession = await _db.UserSessions.FindAsync(sessionId);
        Assert.NotNull(secondSession);
        Assert.Equal(firstUpdatedTime, secondSession.LastActiveAt); // 시간 변화 없음
    }

    [Fact]
    public async Task SessionValidationMiddleware_RevokedSession_SignOutAndRedirectsToLogin()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var sessionId = Guid.NewGuid().ToString("N");
        
        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            UserAgent = "TestAgent",
            IpAddress = "127.0.0.1",
            IsRevoked = true // 취소됨
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("SessionId", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        var context = new DefaultHttpContext { RequestServices = _authController.ControllerContext.HttpContext.RequestServices };
        context.User = new ClaimsPrincipal(identity);

        var isNextCalled = false;
        var middleware = new SessionValidationMiddleware(innerHttpContext =>
        {
            isNextCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context, _db);

        // Assert
        Assert.False(isNextCalled); // next 대리자가 호출되지 않고 차단되어야 함
        Assert.True(_authService.SignedOutCalled); // SignOut 호출 확인
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal("/login?error=session_expired", context.Response.Headers["Location"]);
    }

    #region Fakes for HTTP Auth Mocking
    private class FakeAuthenticationService : IAuthenticationService
    {
        public ClaimsPrincipal? SignedInPrincipal { get; private set; }
        public string? SignedInScheme { get; private set; }
        public bool SignedOutCalled { get; private set; }

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
        {
            SignedInScheme = scheme;
            SignedInPrincipal = principal;
            return Task.CompletedTask;
        }

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            SignedOutCalled = true;
            return Task.CompletedTask;
        }

        public AuthenticateResult? AuthenticateResult { get; set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            return Task.FromResult(AuthenticateResult ?? AuthenticateResult.NoResult());
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
        {
            return Task.CompletedTask;
        }
    }

    private class FakeServiceProvider : IServiceProvider
    {
        private readonly IAuthenticationService _authService;

        public FakeServiceProvider(IAuthenticationService authService)
        {
            _authService = authService;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IAuthenticationService))
                return _authService;
            return null;
        }
    }
    #endregion
}
