using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Aristokeides.Api.Services;

public class LfsService
{
    private readonly IConfiguration _config;
    private readonly string _storageRoot;

    public LfsService(IConfiguration config)
    {
        _config = config;
        // 글로벌 LFS 저장소 디렉토리
        _storageRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GitRepos", "lfs");
    }

    // OID를 기반으로 실제 글로벌 LFS 파일 경로 획득
    public string GetObjectPath(string oid)
    {
        if (string.IsNullOrEmpty(oid) || oid.Length < 4)
            throw new ArgumentException("Invalid OID");

        string folder1 = oid.Substring(0, 2);
        string folder2 = oid.Substring(2, 2);
        return Path.Combine(_storageRoot, "objects", folder1, folder2, oid);
    }

    // 임시 업로드 파일 경로 획득
    public string GetTempPath(string oid)
    {
        string tempDir = Path.Combine(_storageRoot, "temp");
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }
        return Path.Combine(tempDir, $"{oid}_{Guid.NewGuid()}");
    }

    // 파일이 글로벌 스토리지에 존재하고 크기가 일치하는지 확인
    public bool Exists(string oid, long size)
    {
        try
        {
            string path = GetObjectPath(oid);
            if (!File.Exists(path))
                return false;

            var fileInfo = new FileInfo(path);
            return fileInfo.Length == size;
        }
        catch
        {
            return false;
        }
    }

    // 단기 LFS 전용 JWT 토큰 생성 (1시간 유효)
    public string GenerateToken(Guid repositoryId, string oid, string action)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "default_jwt_secret_key_for_aristokeides");
        
        var claims = new[]
        {
            new Claim("repo_id", repositoryId.ToString()),
            new Claim("oid", oid),
            new Claim("action", action)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    // LFS 토큰 검증 및 클레임 반환
    public ClaimsPrincipal? ValidateToken(string token, Guid repositoryId, string oid, string action)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? "default_jwt_secret_key_for_aristokeides");

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _config["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            var tokenRepoId = principal.FindFirst("repo_id")?.Value;
            var tokenOid = principal.FindFirst("oid")?.Value;
            var tokenAction = principal.FindFirst("action")?.Value;

            if (tokenRepoId != repositoryId.ToString() || 
                tokenOid != oid || 
                tokenAction != action)
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
