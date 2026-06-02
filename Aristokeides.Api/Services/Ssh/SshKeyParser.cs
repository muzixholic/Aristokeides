using System;
using System.IO;
using System.Text;

namespace Aristokeides.Api.Services.Ssh;

/// <summary>
/// OpenSSH 공개키 형식을 파싱하고, 보안 요구사항(알고리즘 유형 및 키 길이)을 검증하는 파서 서비스.
/// </summary>
public static class SshKeyParser
{
    public static (string algorithm, int? keySize, string comment) ParseAndValidatePublicKey(string publicKeyContent)
    {
        if (string.IsNullOrWhiteSpace(publicKeyContent))
            throw new ArgumentException("유효하지 않은 SSH 공개키 포맷입니다.");

        // 스페이스로 잘라서 [알고리즘, base64 페이로드, 주석(선택)]으로 분리
        string[] parts = publicKeyContent.Trim().Split(' ', 3);
        if (parts.Length < 2)
            throw new ArgumentException("유효하지 않은 SSH 공개키 포맷입니다.");

        string algorithm = parts[0];
        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            throw new ArgumentException("유효하지 않은 SSH 공개키 포맷입니다.");
        }
        
        string comment = parts.Length > 2 ? parts[2].Trim() : string.Empty;

        // 허용되는 알고리즘 필터링 (Ed25519, ECDSA, RSA-3072+)
        if (algorithm != "ssh-rsa" && algorithm != "ssh-ed25519" && !algorithm.StartsWith("ecdsa-sha2-"))
        {
            throw new NotSupportedException("지원되지 않는 키 유형입니다. Ed25519, ECDSA, RSA-3072+ 알고리즘만 등록 가능합니다.");
        }

        int? keySize = null;
        using (var ms = new MemoryStream(keyBytes))
        {
            string readAlgo = ReadString(ms);
            if (readAlgo != algorithm)
                throw new ArgumentException("키 헤더와 바이너리 메타데이터가 일치하지 않습니다.");

            if (algorithm == "ssh-rsa")
            {
                byte[] exponent = ReadBytes(ms);
                byte[] modulus = ReadBytes(ms);

                // Modulus의 첫 바이트가 0x00이면 부호 방지용이므로 제외
                int modulusLength = modulus.Length;
                if (modulusLength > 0 && modulus[0] == 0x00)
                {
                    modulusLength--;
                }
                keySize = modulusLength * 8;

                if (keySize < 3072)
                {
                    throw new InvalidOperationException($"보안 강도가 취약합니다. RSA 키는 최소 3072비트 이상이어야 합니다. (입력됨: {keySize}비트)");
                }
            }
            else if (algorithm.StartsWith("ecdsa-sha2-"))
            {
                string curve = ReadString(ms);
                keySize = curve switch
                {
                    "nistp256" => 256,
                    "nistp384" => 384,
                    "nistp521" => 521,
                    _ => throw new NotSupportedException($"지원하지 않는 ECDSA 곡선입니다: {curve}")
                };
            }
            else if (algorithm == "ssh-ed25519")
            {
                keySize = 256; // Ed25519 고정 크기
            }
        }

        return (algorithm, keySize, comment);
    }

    private static string ReadString(Stream stream)
    {
        byte[] lenBytes = new byte[4];
        if (stream.Read(lenBytes, 0, 4) != 4) throw new EndOfStreamException();
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        uint len = BitConverter.ToUInt32(lenBytes, 0);

        byte[] valBytes = new byte[len];
        if (stream.Read(valBytes, 0, (int)len) != len) throw new EndOfStreamException();
        return Encoding.ASCII.GetString(valBytes);
    }

    private static byte[] ReadBytes(Stream stream)
    {
        byte[] lenBytes = new byte[4];
        if (stream.Read(lenBytes, 0, 4) != 4) throw new EndOfStreamException();
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        uint len = BitConverter.ToUInt32(lenBytes, 0);

        byte[] valBytes = new byte[len];
        if (stream.Read(valBytes, 0, (int)len) != len) throw new EndOfStreamException();
        return valBytes;
    }
}
