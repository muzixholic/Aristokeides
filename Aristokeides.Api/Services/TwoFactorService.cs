using System;
using System.Collections.Generic;
using System.Linq;
using OtpNet;

namespace Aristokeides.Api.Services;

/// <summary>
/// 2단계 인증(2FA)을 위한 TOTP 비밀키 생성, OTP 코드 검증 및 복구 코드 관리를 제공하는 서비스입니다.
/// </summary>
public class TwoFactorService
{
    /// <summary>
    /// TOTP용 임의의 20바이트 보안 비밀키를 생성하고 Base32 문자열로 변환하여 반환합니다.
    /// </summary>
    public string GenerateSecretKey()
    {
        byte[] keyBytes = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(keyBytes);
    }

    /// <summary>
    /// Base32로 인코딩된 비밀키와 사용자가 입력한 6자리 TOTP 코드를 검증합니다.
    /// </summary>
    public bool VerifyTotp(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        try
        {
            byte[] secretBytes = Base32Encoding.ToBytes(secret);
            var totp = new Totp(secretBytes);
            // RFC 6238 시간 보정 및 네트워크 지연 대응을 위해 앞뒤로 1개 시간 스텝(30초) 오차를 허용합니다.
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 10개의 임의 복구 코드를 생성합니다. (각 코드는 10자리의 영문 대문자 및 숫자로 구성됨)
    /// </summary>
    public string[] GenerateRecoveryCodes()
    {
        var codes = new string[10];
        for (int i = 0; i < 10; i++)
        {
            codes[i] = GenerateSingleRecoveryCode();
        }
        return codes;
    }

    /// <summary>
    /// 사용자가 입력한 복구 코드를 검증하고, 유효한 경우 목록에서 해당 코드를 제거합니다.
    /// </summary>
    /// <param name="recoveryCodesString">쉼표로 구분된 기존 복구 코드 문자열</param>
    /// <param name="inputCode">사용자가 입력한 코드</param>
    /// <param name="updatedRecoveryCodesString">코드가 제거된 후 업데이트된 문자열</param>
    /// <returns>검증 성공 여부</returns>
    public bool VerifyAndConsumeRecoveryCode(string? recoveryCodesString, string inputCode, out string? updatedRecoveryCodesString)
    {
        updatedRecoveryCodesString = recoveryCodesString;
        if (string.IsNullOrWhiteSpace(recoveryCodesString) || string.IsNullOrWhiteSpace(inputCode))
        {
            return false;
        }

        var codes = recoveryCodesString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                       .Select(c => c.Trim().ToUpperInvariant())
                                       .ToList();

        var normalizedInput = inputCode.Trim().ToUpperInvariant();
        if (codes.Contains(normalizedInput))
        {
            codes.Remove(normalizedInput);
            updatedRecoveryCodesString = codes.Count > 0 ? string.Join(",", codes) : string.Empty;
            return true;
        }

        return false;
    }

    private string GenerateSingleRecoveryCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
