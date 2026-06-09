using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Aristokeides.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// 신규 사용자 등록. 비밀번호는 BCrypt로 해시.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            return Conflict(new { message = "이미 등록된 이메일입니다." });

        if (await _db.Users.AnyAsync(u => u.Username == request.Username))
            return Conflict(new { message = "이미 등록된 사용자명입니다." });

        var user = new User
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Reader" // 기본 역할
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new { id = user.Id },
            new { user.Id, user.Email, user.Role });
    }

    /// <summary>
    /// 로그인 후 JWT 토큰 발급. Role claim이 토큰에 포함됨.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "이메일 또는 비밀번호가 올바르지 않습니다." });

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }



    [HttpPost("cookie-register")]
    public async Task<IActionResult> CookieRegister(
        [FromForm] string email,
        [FromForm] string username,
        [FromForm] string password)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Redirect("/register?error=duplicate_email");

        if (await _db.Users.AnyAsync(u => u.Username == username))
            return Redirect("/register?error=duplicate_username");

        var user = new User
        {
            Email = email,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = "Reader"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Redirect("/login?registered=true");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionIdClaim = User.FindFirst("SessionId");
        if (sessionIdClaim != null)
        {
            var session = await _db.UserSessions.FindAsync(sessionIdClaim.Value);
            if (session != null)
            {
                session.IsRevoked = true;
                await _db.SaveChangesAsync();
            }
        }
        await HttpContext.SignOutAsync("Cookies");
        return Redirect("/");
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost("cookie-login")]
    public async Task<IActionResult> CookieLogin([FromForm] string email, [FromForm] string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return Redirect("/login?error=invalid_credentials");

        if (user.IsTwoFactorEnabled)
        {
            // 임시 2FA 세션 발급
            var tempClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("amr", "2fa_pending")
            };
            var tempIdentity = new ClaimsIdentity(tempClaims, "Cookies");
            var tempPrincipal = new ClaimsPrincipal(tempIdentity);

            await HttpContext.SignInAsync("Cookies", tempPrincipal, new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                IsPersistent = false
            });

            return Redirect("/login-2fa");
        }

        var sessionId = Guid.NewGuid().ToString("N");
        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("SessionId", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", principal);
        return Redirect("/");
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("2fa/status")]
    public async Task<IActionResult> Get2FaStatus()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "인증되지 않은 사용자입니다." });

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "사용자를 찾을 수 없습니다." });

        return Ok(new { isEnabled = user.IsTwoFactorEnabled });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("2fa/setup")]
    public async Task<IActionResult> Setup2Fa([FromServices] TwoFactorService tfaService)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "인증되지 않은 사용자입니다." });

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "사용자를 찾을 수 없습니다." });

        var secret = tfaService.GenerateSecretKey();
        var otpAuthUri = $"otpauth://totp/Aristokeides:{user.Email}?secret={secret}&issuer=Aristokeides";

        return Ok(new { secret, otpAuthUri });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> Enable2Fa([FromBody] Enable2FaRequest request, [FromServices] TwoFactorService tfaService)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "인증되지 않은 사용자입니다." });

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "사용자를 찾을 수 없습니다." });

        if (!tfaService.VerifyTotp(request.Secret, request.Code))
        {
            return BadRequest(new { message = "OTP 코드가 올바르지 않습니다." });
        }

        user.TwoFactorSecret = request.Secret;
        user.IsTwoFactorEnabled = true;

        var recoveryCodes = tfaService.GenerateRecoveryCodes();
        user.TwoFactorRecoveryCodes = string.Join(",", recoveryCodes);

        await _db.SaveChangesAsync();

        return Ok(new { recoveryCodes });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> Disable2Fa([FromBody] Disable2FaRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "인증되지 않은 사용자입니다." });

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "사용자를 찾을 수 없습니다." });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return BadRequest(new { message = "비밀번호가 올바르지 않습니다." });
        }

        user.IsTwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.TwoFactorRecoveryCodes = null;

        await _db.SaveChangesAsync();

        return Ok(new { message = "2FA가 성공적으로 비활성화되었습니다." });
    }

    [HttpPost("2fa/verify")]
    public async Task<IActionResult> Verify2Fa([FromForm] string code, [FromServices] TwoFactorService tfaService)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync("Cookies");
        if (!authenticateResult.Succeeded)
            return Redirect("/login?error=timeout");

        var principal = authenticateResult.Principal;
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        var amrClaim = principal.FindFirst("amr");

        if (userIdClaim == null || amrClaim?.Value != "2fa_pending")
            return Redirect("/login?error=invalid_state");

        if (!int.TryParse(userIdClaim.Value, out int userId))
            return Redirect("/login?error=invalid_state");

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return Redirect("/login?error=user_not_found");

        bool isValid = false;
        bool isRecoveryCodeUsed = false;
        string? updatedRecoveryCodes = null;

        if (code != null && code.Trim().Length == 6 && int.TryParse(code.Trim(), out _))
        {
            isValid = tfaService.VerifyTotp(user.TwoFactorSecret!, code.Trim());
        }
        else if (code != null)
        {
            isValid = tfaService.VerifyAndConsumeRecoveryCode(user.TwoFactorRecoveryCodes, code.Trim(), out updatedRecoveryCodes);
            isRecoveryCodeUsed = isValid;
        }

        if (!isValid)
        {
            return Redirect("/login-2fa?error=invalid_code");
        }

        if (isRecoveryCodeUsed)
        {
            user.TwoFactorRecoveryCodes = updatedRecoveryCodes;
            await _db.SaveChangesAsync();
        }

        await HttpContext.SignOutAsync("Cookies");

        var sessionId = Guid.NewGuid().ToString("N");
        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("SessionId", sessionId)
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        var newPrincipal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", newPrincipal);
        return Redirect("/");
    }

    /// <summary>
    /// 외부 로그인 요청 엔드포인트
    /// </summary>
    [HttpGet("external-login")]
    public IActionResult ExternalLogin([FromQuery] string provider, [FromQuery] string? redirectUrl = "/")
    {
        var properties = new AuthenticationProperties { RedirectUri = Url.Action(nameof(ExternalLoginCallback), new { redirectUrl }) };
        return Challenge(properties, provider);
    }

    /// <summary>
    /// 외부 로그인 콜백 엔드포인트
    /// </summary>
    [HttpGet("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallback([FromQuery] string? redirectUrl = "/")
    {
        var authResult = await HttpContext.AuthenticateAsync("Cookies");
        if (!authResult.Succeeded || authResult.Principal == null)
        {
            return Redirect("/login?error=external_auth_failed");
        }

        var principal = authResult.Principal;
        var providerKey = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value;
        var name = principal.FindFirst(ClaimTypes.Name)?.Value ?? email?.Split('@')[0] ?? "socialuser";

        // 공급자 정보 가져오기
        var provider = principal.Identity?.AuthenticationType;
        if (string.IsNullOrEmpty(provider))
        {
            var issuer = principal.FindFirst(ClaimTypes.NameIdentifier)?.Issuer?.ToLowerInvariant() ?? "";
            if (issuer.Contains("github")) provider = "GitHub";
            else if (issuer.Contains("google")) provider = "Google";
            else provider = "External";
        }

        if (string.IsNullOrEmpty(providerKey))
        {
            return Redirect("/login?error=invalid_external_claims");
        }

        // 1. 소셜 로그인 매핑 데이터 조회
        var socialLogin = await _db.UserSocialLogins
            .Include(us => us.User)
            .FirstOrDefaultAsync(us => us.Provider == provider && us.ProviderKey == providerKey);

        User? user = null;

        if (socialLogin != null)
        {
            user = socialLogin.User;
        }
        else
        {
            // 2. 동일한 이메일을 가진 기존 사용자 검색
            if (!string.IsNullOrEmpty(email))
            {
                user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            }

            if (user != null)
            {
                // 기존 사용자에 대해 소셜 매핑 추가
                var newMapping = new UserSocialLogin
                {
                    UserId = user.Id,
                    Provider = provider,
                    ProviderKey = providerKey
                };
                _db.UserSocialLogins.Add(newMapping);
                await _db.SaveChangesAsync();
            }
            else
            {
                // 3. 신규 사용자 생성 (이메일 및 사용자명 유니크 처리)
                if (string.IsNullOrEmpty(email))
                {
                    email = $"{providerKey}@{provider.ToLowerInvariant()}.local";
                }

                if (await _db.Users.AnyAsync(u => u.Email == email))
                {
                    email = $"{providerKey}_{Guid.NewGuid().ToString("N")[..8]}@{provider.ToLowerInvariant()}.local";
                }

                var baseUsername = name.Replace(" ", "_");
                var username = baseUsername;
                int suffix = 1;
                while (await _db.Users.AnyAsync(u => u.Username == username))
                {
                    username = $"{baseUsername}_{suffix++}";
                }

                user = new User
                {
                    Email = email,
                    Username = username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N")),
                    Role = "Reader" // 안전한 기본 가입 역할
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                var newMapping = new UserSocialLogin
                {
                    UserId = user.Id,
                    Provider = provider,
                    ProviderKey = providerKey
                };
                _db.UserSocialLogins.Add(newMapping);
                await _db.SaveChangesAsync();
            }
        }

        // 임시 로그인 정보 제거
        await HttpContext.SignOutAsync("Cookies");

        // 4. 2FA 필수 여부 검증
        if (user.IsTwoFactorEnabled)
        {
            var tempClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("amr", "2fa_pending")
            };
            var tempIdentity = new ClaimsIdentity(tempClaims, "Cookies");
            var tempPrincipal = new ClaimsPrincipal(tempIdentity);

            await HttpContext.SignInAsync("Cookies", tempPrincipal, new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5),
                IsPersistent = false
            });

            return Redirect("/login-2fa");
        }

        // 5. 성공적인 로그인 쿠키 발행
        var sessionId = Guid.NewGuid().ToString("N");
        var session = new UserSession
        {
            Id = sessionId,
            UserId = user.Id,
            UserAgent = Request.Headers.UserAgent.ToString(),
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };
        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("SessionId", sessionId)
        };
        var loginIdentity = new ClaimsIdentity(claims, "Cookies");
        var loginPrincipal = new ClaimsPrincipal(loginIdentity);

        await HttpContext.SignInAsync("Cookies", loginPrincipal);
        return Redirect(redirectUrl ?? "/");
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("sessions")]
    public async Task<IActionResult> GetActiveSessions()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "인증되지 않은 사용자입니다." });

        var sessions = await _db.UserSessions
            .Where(s => s.UserId == userId && !s.IsRevoked)
            .OrderByDescending(s => s.LastActiveAt)
            .Select(s => new {
                s.Id,
                s.UserAgent,
                s.IpAddress,
                s.CreatedAt,
                s.LastActiveAt
            })
            .ToListAsync();

        return Ok(sessions);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("sessions/revoke")]
    public async Task<IActionResult> RevokeSession([FromBody] RevokeSessionRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(new { message = "인증되지 않은 사용자입니다." });

        var session = await _db.UserSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId);
        if (session == null)
            return NotFound(new { message = "세션을 찾을 수 없습니다." });

        session.IsRevoked = true;
        await _db.SaveChangesAsync();

        return Ok(new { message = "세션이 무효화되었습니다." });
    }
}

public record RegisterRequest(string Email, string Username, string Password);
public record LoginRequest(string Email, string Password);
public record Enable2FaRequest(string Secret, string Code);
public record Disable2FaRequest(string Password);
public record RevokeSessionRequest(string SessionId);
