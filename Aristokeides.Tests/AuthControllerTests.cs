using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Aristokeides.Api.Controllers;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aristokeides.Tests
{
    public class AuthControllerTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly FakeAuthenticationService _authService;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            // InMemory Database Setup
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"Aristokeides_Auth_Test_{Guid.NewGuid()}")
                .Options;
            _db = new AppDbContext(options);

            // Configuration Setup
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", "super_secret_key_that_is_long_enough_for_hmac_sha256_verification_1234567890"},
                {"Jwt:Issuer", "test_issuer"},
                {"Jwt:Audience", "test_audience"}
            };
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            // Authentication Service Fake Setup
            _authService = new FakeAuthenticationService();
            var serviceProvider = new FakeServiceProvider(_authService);

            // HttpContext Setup
            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider;

            // Controller Setup
            _controller = new AuthController(_db, _config)
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

        [Fact]
        public async Task CookieRegister_CreatesUser_WhenValid()
        {
            // Act
            var result = await _controller.CookieRegister("newuser@test.com", "newuser", "password123");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/login?registered=true", redirectResult.Url);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
            Assert.NotNull(user);
            Assert.Equal("newuser", user.Username);
            Assert.Equal("Reader", user.Role);
            Assert.True(BCrypt.Net.BCrypt.Verify("password123", user.PasswordHash));
        }

        [Fact]
        public async Task CookieRegister_RedirectsToError_WhenEmailDuplicate()
        {
            // Arrange: Pre-exist user
            var existingUser = new User
            {
                Email = "duplicate@test.com",
                Username = "existing",
                PasswordHash = "hash",
                Role = "Reader"
            };
            _db.Users.Add(existingUser);
            await _db.SaveChangesAsync();

            // Act
            var result = await _controller.CookieRegister("duplicate@test.com", "otheruser", "password123");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/register?error=duplicate_email", redirectResult.Url);
        }

        [Fact]
        public async Task CookieRegister_RedirectsToError_WhenUsernameDuplicate()
        {
            // Arrange: Pre-exist user
            var existingUser = new User
            {
                Email = "other@test.com",
                Username = "duplicate_name",
                PasswordHash = "hash",
                Role = "Reader"
            };
            _db.Users.Add(existingUser);
            await _db.SaveChangesAsync();

            // Act
            var result = await _controller.CookieRegister("new@test.com", "duplicate_name", "password123");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/register?error=duplicate_username", redirectResult.Url);
        }

        [Fact]
        public async Task CookieLogin_SignInAndRedirectsToHome_WhenCredentialsValid()
        {
            // Arrange
            var user = new User
            {
                Email = "loginuser@test.com",
                Username = "loginuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("validpassword"),
                Role = "Contributor"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Act
            var result = await _controller.CookieLogin("loginuser@test.com", "validpassword");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/", redirectResult.Url);

            Assert.Equal("Cookies", _authService.SignedInScheme);
            Assert.NotNull(_authService.SignedInPrincipal);

            var identity = Assert.IsType<ClaimsIdentity>(_authService.SignedInPrincipal.Identity);
            Assert.Equal("Cookies", identity.AuthenticationType);
            Assert.Equal("loginuser", identity.Name);
            Assert.Equal("loginuser@test.com", identity.FindFirst(ClaimTypes.Email)?.Value);
            Assert.Equal("Contributor", identity.FindFirst(ClaimTypes.Role)?.Value);
        }

        [Fact]
        public async Task CookieLogin_RedirectsToLoginWithError_WhenCredentialsInvalid()
        {
            // Arrange
            var user = new User
            {
                Email = "loginuser@test.com",
                Username = "loginuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("validpassword"),
                Role = "Contributor"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Act
            var result = await _controller.CookieLogin("loginuser@test.com", "wrongpassword");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/login?error=invalid_credentials", redirectResult.Url);
            Assert.Null(_authService.SignedInScheme);
        }

        [Fact]
        public async Task Logout_SignOutAndRedirectsToHome()
        {
            // Act
            var result = await _controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/", redirectResult.Url);
            Assert.True(_authService.SignedOutCalled);
        }
    }

    #region Fakes for HTTP Auth Mocking
    public class FakeAuthenticationService : IAuthenticationService
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

    public class FakeServiceProvider : IServiceProvider
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
