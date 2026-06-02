using System;
using System.Security.Cryptography;

namespace Aristokeides.Api.Services.Ssh;

/// <summary>
/// SSH 공개키 내용으로부터 SHA-256 지문(Fingerprint)을 계산하는 클래스.
/// </summary>
public static class SshFingerprintCalculator
{
    public static string CalculateSha256Fingerprint(string publicKeyContent)
    {
        if (string.IsNullOrWhiteSpace(publicKeyContent))
            throw new ArgumentException("유효하지 않은 SSH 공개키 포맷입니다.");

        string[] parts = publicKeyContent.Trim().Split(' ');
        if (parts.Length < 2)
            throw new ArgumentException("유효하지 않은 SSH 공개키 포맷입니다.");

        string base64Payload = parts[1];
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(base64Payload);
        }
        catch (FormatException)
        {
            throw new ArgumentException("유효하지 않은 SSH 공개키 포맷입니다.");
        }

        byte[] hashBytes = SHA256.HashData(keyBytes);
        
        // base64로 인코딩한 뒤, 패딩 '=' 문자 제거
        string base64Hash = Convert.ToBase64String(hashBytes).TrimEnd('=');
        
        return $"SHA256:{base64Hash}";
    }
}
