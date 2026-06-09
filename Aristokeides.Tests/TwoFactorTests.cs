using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Aristokeides.Api.Controllers;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OtpNet;
using Xunit;

namespace Aristokeides.Tests
{
    public class TwoFactorTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly TwoFactorService _tfaService;
        private readonly FakeAuthenticationService _authService;
        private readonly AuthController _controller;

        public TwoFactorTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"Aristokeides_2FA_Test_{Guid.NewGuid()}")
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

            _tfaService = new TwoFactorService();
            _authService = new FakeAuthenticationService();
            
            var serviceProvider = new FakeServiceProvider(_authService);
            var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };

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
        public void TwoFactorService_GenerateSecretKey_ReturnsValidBase32String()
        {
            // Act
            var secret = _tfaService.GenerateSecretKey();

            // Assert
            Assert.NotNull(secret);
            Assert.NotEmpty(secret);
            var bytes = Base32Encoding.ToBytes(secret);
            Assert.Equal(20, bytes.Length);
        }

        [Fact]
        public void TwoFactorService_VerifyTotp_ValidatesCorrectCode()
        {
            // Arrange
            var secret = _tfaService.GenerateSecretKey();
            var bytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(bytes);
            var correctCode = totp.ComputeTotp();

            // Act
            var isValid = _tfaService.VerifyTotp(secret, correctCode);
            var isInvalid = _tfaService.VerifyTotp(secret, "000000");

            // Assert
            Assert.True(isValid);
            Assert.False(isInvalid);
        }

        [Fact]
        public void TwoFactorService_RecoveryCodes_GenerationAndConsumption()
        {
            // Arrange
            var codes = _tfaService.GenerateRecoveryCodes();
            Assert.Equal(10, codes.Length);
            foreach (var code in codes)
            {
                Assert.Equal(10, code.Length);
            }

            var codesStr = string.Join(",", codes);
            var targetCode = codes[0];

            // Act: 소비 성공
            var consumeResult = _tfaService.VerifyAndConsumeRecoveryCode(codesStr, targetCode, out var updatedCodes);
            // Act: 중복 소비 실패
            var consumeResultDuplicate = _tfaService.VerifyAndConsumeRecoveryCode(updatedCodes, targetCode, out _);
            // Act: 잘못된 코드 소비 실패
            var consumeResultInvalid = _tfaService.VerifyAndConsumeRecoveryCode(updatedCodes, "INVALIDCODE", out _);

            // Assert
            Assert.True(consumeResult);
            Assert.False(consumeResultDuplicate);
            Assert.False(consumeResultInvalid);
            
            var updatedList = updatedCodes?.Split(',', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(9, updatedList?.Length);
            Assert.DoesNotContain(targetCode, updatedList ?? Array.Empty<string>());
        }

        [Fact]
        public async Task CookieLogin_RedirectsToHome_When2FaIsDisabled()
        {
            // Arrange: 2FA가 비활성화된 유저 생성
            var user = new User
            {
                Email = "user_no_2fa@test.com",
                Username = "no2fa",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Reader",
                IsTwoFactorEnabled = false
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Act
            var result = await _controller.CookieLogin("user_no_2fa@test.com", "password123");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/", redirectResult.Url);
            Assert.NotNull(_authService.SignedInPrincipal);
            Assert.Null(_authService.SignedInPrincipal.FindFirst("amr"));
        }

        [Fact]
        public async Task CookieLogin_RedirectsToLogin2Fa_When2FaIsEnabled()
        {
            // Arrange: 2FA가 활성화된 유저 생성
            var user = new User
            {
                Email = "user_2fa@test.com",
                Username = "has2fa",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Reader",
                IsTwoFactorEnabled = true,
                TwoFactorSecret = "MYSECRETKEYBASE32"
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Act
            var result = await _controller.CookieLogin("user_2fa@test.com", "password123");

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/login-2fa", redirectResult.Url);
            Assert.NotNull(_authService.SignedInPrincipal);
            Assert.Equal("2fa_pending", _authService.SignedInPrincipal.FindFirst("amr")?.Value);
        }

        [Fact]
        public async Task Verify2Fa_Succeeds_WithCorrectOtp()
        {
            // Arrange: 2FA 활성 유저 등록
            var secret = _tfaService.GenerateSecretKey();
            var user = new User
            {
                Email = "user_verify@test.com",
                Username = "verify2fa",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Reader",
                IsTwoFactorEnabled = true,
                TwoFactorSecret = secret
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 임시 세션 셋업 (2fa_pending)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("amr", "2fa_pending")
            };
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Cookies");

            _authService.AuthenticateResult = AuthenticateResult.Success(ticket);

            var totp = new Totp(Base32Encoding.ToBytes(secret));
            var correctCode = totp.ComputeTotp();

            // Act
            var result = await _controller.Verify2Fa(correctCode, _tfaService);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/", redirectResult.Url);
            Assert.NotNull(_authService.SignedInPrincipal);
            Assert.Null(_authService.SignedInPrincipal.FindFirst("amr"));
        }

        [Fact]
        public async Task Verify2Fa_Succeeds_WithRecoveryCode()
        {
            // Arrange: 2FA 활성 유저 등록
            var secret = _tfaService.GenerateSecretKey();
            var recoveryCodes = _tfaService.GenerateRecoveryCodes();
            var user = new User
            {
                Email = "user_recovery@test.com",
                Username = "recovery2fa",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = "Reader",
                IsTwoFactorEnabled = true,
                TwoFactorSecret = secret,
                TwoFactorRecoveryCodes = string.Join(",", recoveryCodes)
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 임시 세션 셋업
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("amr", "2fa_pending")
            };
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Cookies");

            _authService.AuthenticateResult = AuthenticateResult.Success(ticket);

            var firstRecoveryCode = recoveryCodes[0];

            // Act
            var result = await _controller.Verify2Fa(firstRecoveryCode, _tfaService);

            // Assert
            var redirectResult = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/", redirectResult.Url);
            
            var updatedUser = await _db.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.DoesNotContain(firstRecoveryCode, updatedUser.TwoFactorRecoveryCodes ?? "");
        }
    }
}
