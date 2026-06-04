using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Aristokeides.Api.Services.Ssh;

/// <summary>
/// OpenSSH 서명 바이너리(SSHSIG) 파서.
/// </summary>
public static class SshSignatureParser
{
    public static (byte[] publicKeyBytes, string algorithm, string fingerprint) ParseSignature(string signatureText)
    {
        if (string.IsNullOrWhiteSpace(signatureText))
            throw new ArgumentException("서명 텍스트가 비어 있습니다.");

        var lines = signatureText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var base64Sb = new StringBuilder();
        bool insideSignature = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed == "-----BEGIN SSH SIGNATURE-----")
            {
                insideSignature = true;
                continue;
            }
            if (trimmed == "-----END SSH SIGNATURE-----")
            {
                insideSignature = false;
                break;
            }
            if (insideSignature)
            {
                base64Sb.Append(trimmed);
            }
        }

        string base64String = base64Sb.ToString();
        if (string.IsNullOrEmpty(base64String))
        {
            throw new ArgumentException("유효하지 않은 SSH 서명 포맷입니다. (BEGIN/END SSH SIGNATURE 누락)");
        }

        byte[] sigBytes;
        try
        {
            sigBytes = Convert.FromBase64String(base64String);
        }
        catch (FormatException)
        {
            throw new ArgumentException("유효하지 않은 Base64 인코딩 서명 데이터입니다.");
        }

        using (var ms = new MemoryStream(sigBytes))
        {
            // Magic String "SSHSIG" (6바이트) 검증
            byte[] magic = new byte[6];
            if (ms.Read(magic, 0, 6) != 6) throw new ArgumentException("서명 데이터가 너무 짧습니다.");
            string magicStr = Encoding.ASCII.GetString(magic);
            if (magicStr != "SSHSIG")
            {
                throw new ArgumentException("유효하지 않은 SSHSIG 매직 헤더입니다.");
            }

            // Version (4바이트, uint32, 빅엔디안)
            byte[] versionBytes = new byte[4];
            if (ms.Read(versionBytes, 0, 4) != 4) throw new ArgumentException("서명 데이터 버전 정보를 읽을 수 없습니다.");
            if (BitConverter.IsLittleEndian) Array.Reverse(versionBytes);
            uint version = BitConverter.ToUInt32(versionBytes, 0);
            if (version != 1)
            {
                throw new ArgumentException($"지원하지 않는 SSHSIG 버전입니다: {version}");
            }

            // Public Key (string: 4바이트 길이 + 바이트 데이터)
            byte[] pkBytes = ReadBytes(ms);

            // 공개키의 내부 구조로부터 알고리즘 파싱
            string algorithm = ParseAlgorithmFromPublicKey(pkBytes);

            // Fingerprint 구하기
            byte[] hashBytes = SHA256.HashData(pkBytes);
            string base64Hash = Convert.ToBase64String(hashBytes).TrimEnd('=');
            string fingerprint = $"SHA256:{base64Hash}";

            return (pkBytes, algorithm, fingerprint);
        }
    }

    private static string ParseAlgorithmFromPublicKey(byte[] pkBytes)
    {
        using (var ms = new MemoryStream(pkBytes))
        {
            byte[] lenBytes = new byte[4];
            if (ms.Read(lenBytes, 0, 4) != 4) throw new ArgumentException("공개키 알고리즘 길이를 읽을 수 없습니다.");
            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
            uint len = BitConverter.ToUInt32(lenBytes, 0);

            byte[] algoBytes = new byte[len];
            if (ms.Read(algoBytes, 0, (int)len) != len) throw new ArgumentException("공개키 알고리즘 명칭을 읽을 수 없습니다.");
            return Encoding.ASCII.GetString(algoBytes);
        }
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
