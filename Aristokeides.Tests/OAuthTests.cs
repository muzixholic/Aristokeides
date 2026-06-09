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
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aristokeides.Tests
{
    public class OAuthTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly FakeAuthenticationService _authService;
        private readonly AuthController _controller;

        public OAuthTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"Aristokeides_OAuth_Test_{Guid.NewGuid()}")
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

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = serviceProvider;

            _controller = new AuthController(_db, _config)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                },
                Url = new FakeUrlHelper()
            };
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        [Fact]
        public void ExternalLogin_ReturnsChallengeResult()
        {
            // Act
            var result = _controller.ExternalLogin("Google", "/dashboard");

            // Assert
            var challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Contains("Google", challengeResult.AuthenticationSchemes);
            Assert.Equal("/api/Auth/ExternalLoginCallback?redirectUrl=%2Fdashboard", challengeResult.Properties?.RedirectUri);
        }

        [Fact]
        public async Task ExternalLoginCallback_LogsInExistingMappedUser()
        {
            // Arrange: 가입된 유저 및 소셜 매핑 추가
            var user = new User
            {
                Email = "existing_social@test.com",
                Username = "social_user",
                PasswordHash = "hash",
                Role = "Contributor"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var socialMapping = new UserSocialLogin
            {
                UserId = user.Id,
                Provider = "Google",
                ProviderKey = "google_12345"
            };
            _db.UserSocialLogins.Add(socialMapping);
            await _db.SaveChangesAsync();

            // HTTP 외부 인증 Mocking (Cookies 스키마로 인증 통과)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "google_12345"),
                new Claim(ClaimTypes.Email, "existing_social@test.com"),
                new Claim(ClaimTypes.Name, "Google User")
            };
            var identity = new ClaimsIdentity(claims, "Google"); // AuthenticationType = Google
            var principal = new ClaimsPrincipal(identity);
            
            var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), "Cookies");
            _authService.AuthenticateResult = AuthenticateResult.Success(ticket);

            // Act
            var result = await _controller.ExternalLoginCallback("/target");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/target", redirectResult.Url);

            // 로그인 정보 검증
            Assert.Equal("Cookies", _authService.SignedInScheme);
            Assert.NotNull(_authService.SignedInPrincipal);
            var loginIdentity = Assert.IsType<ClaimsIdentity>(_authService.SignedInPrincipal.Identity);
            Assert.Equal(user.Id.ToString(), loginIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            Assert.Equal("Contributor", loginIdentity.FindFirst(ClaimTypes.Role)?.Value);
        }

        [Fact]
        public async Task ExternalLoginCallback_LinksExistingEmailUser_WhenMappingDoesNotExist()
        {
            // Arrange: 이메일은 같으나 소셜 매핑이 없는 기존 유저
            var user = new User
            {
                Email = "link_me@test.com",
                Username = "link_me",
                PasswordHash = "hash",
                Role = "Contributor"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "github_67890"),
                new Claim(ClaimTypes.Email, "link_me@test.com"),
                new Claim(ClaimTypes.Name, "GitHub User")
            };
            var identity = new ClaimsIdentity(claims, "GitHub");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), "Cookies");
            _authService.AuthenticateResult = AuthenticateResult.Success(ticket);

            // Act
            var result = await _controller.ExternalLoginCallback("/target");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/target", redirectResult.Url);

            // 매핑이 새로 생겼는지 검증
            var mapping = await _db.UserSocialLogins.FirstOrDefaultAsync(m => m.UserId == user.Id);
            Assert.NotNull(mapping);
            Assert.Equal("GitHub", mapping.Provider);
            Assert.Equal("github_67890", mapping.ProviderKey);

            // 로그인 여부 검증
            Assert.Equal("Cookies", _authService.SignedInScheme);
            var loginIdentity = Assert.IsType<ClaimsIdentity>(_authService.SignedInPrincipal?.Identity);
            Assert.Equal("Contributor", loginIdentity.FindFirst(ClaimTypes.Role)?.Value);
        }

        [Fact]
        public async Task ExternalLoginCallback_AutoRegistersNewUser_WithReaderRole()
        {
            // Arrange: 정보 없는 소셜 신규 회원 가입
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "github_fresh"),
                new Claim(ClaimTypes.Email, "fresh_oauth@test.com"),
                new Claim(ClaimTypes.Name, "Fresh OAuth User")
            };
            var identity = new ClaimsIdentity(claims, "GitHub");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), "Cookies");
            _authService.AuthenticateResult = AuthenticateResult.Success(ticket);

            // Act
            var result = await _controller.ExternalLoginCallback("/target");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/target", redirectResult.Url);

            // 유저 신규 생성 검증
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == "fresh_oauth@test.com");
            Assert.NotNull(user);
            Assert.Equal("Reader", user.Role); // 최초 가입 시 Reader 역할 필수
            Assert.Equal("Fresh_OAuth_User", user.Username);

            // 매핑 생성 검증
            var mapping = await _db.UserSocialLogins.FirstOrDefaultAsync(m => m.UserId == user.Id);
            Assert.NotNull(mapping);
            Assert.Equal("GitHub", mapping.Provider);
            Assert.Equal("github_fresh", mapping.ProviderKey);
        }

        [Fact]
        public async Task ExternalLoginCallback_RedirectsTo2FaPending_When2FaIsEnabled()
        {
            // Arrange: 2FA가 활성화된 소셜 연동 유저
            var user = new User
            {
                Email = "mfa_social@test.com",
                Username = "mfa_social",
                PasswordHash = "hash",
                Role = "Contributor",
                IsTwoFactorEnabled = true,
                TwoFactorSecret = "SECRET"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var socialMapping = new UserSocialLogin
            {
                UserId = user.Id,
                Provider = "Google",
                ProviderKey = "google_mfa"
            };
            _db.UserSocialLogins.Add(socialMapping);
            await _db.SaveChangesAsync();

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "google_mfa"),
                new Claim(ClaimTypes.Email, "mfa_social@test.com"),
                new Claim(ClaimTypes.Name, "Mfa User")
            };
            var identity = new ClaimsIdentity(claims, "Google");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, new AuthenticationProperties(), "Cookies");
            _authService.AuthenticateResult = AuthenticateResult.Success(ticket);

            // Act
            var result = await _controller.ExternalLoginCallback("/target");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/login-2fa", redirectResult.Url);

            // 2FA pending 임시 클레임 발급 상태인지 검증
            Assert.Equal("Cookies", _authService.SignedInScheme);
            var tempIdentity = Assert.IsType<ClaimsIdentity>(_authService.SignedInPrincipal?.Identity);
            Assert.Equal("2fa_pending", tempIdentity.FindFirst("amr")?.Value);
        }
    }

    #region Fake UrlHelper for testing
    public class FakeUrlHelper : IUrlHelper
    {
        public string? Action(UrlActionContext actionContext)
        {
            var routeValues = actionContext.Values as Microsoft.AspNetCore.Routing.RouteValueDictionary 
                ?? new Microsoft.AspNetCore.Routing.RouteValueDictionary(actionContext.Values);
            routeValues.TryGetValue("redirectUrl", out var redirectUrl);
            return $"/api/Auth/{actionContext.Action}?redirectUrl={Uri.EscapeDataString(redirectUrl?.ToString() ?? "")}";
        }

        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => null;
        public string? RouteUrl(UrlRouteContext routeContext) => null;

        public ActionContext ActionContext => new ActionContext();
    }
    #endregion
}
