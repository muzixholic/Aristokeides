using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Aristokeides.Api.Services.Ssh;
using Xunit;

namespace Aristokeides.Tests;

public class SshKeyParserTests
{
    // RSA SSH 공개키를 OpenSSH 포맷으로 동적 생성하는 헬퍼
    private static string GenerateOpenSshRsaKey(int keySize, string comment = "test@comment")
    {
        using var rsa = RSA.Create(keySize);
        var parameters = rsa.ExportParameters(false);
        using var ms = new MemoryStream();
        
        WriteNextString(ms, "ssh-rsa");
        WriteNextBytes(ms, parameters.Exponent ?? Array.Empty<byte>());
        
        byte[] modulus = parameters.Modulus ?? Array.Empty<byte>();
        // SSH 정수 형식은 첫 비트가 1이면 부호 방지용 0x00을 패딩함
        if (modulus.Length > 0 && modulus[0] >= 0x80)
        {
            byte[] temp = new byte[modulus.Length + 1];
            Array.Copy(modulus, 0, temp, 1, modulus.Length);
            modulus = temp;
        }
        WriteNextBytes(ms, modulus);

        string base64 = Convert.ToBase64String(ms.ToArray());
        return $"ssh-rsa {base64} {comment}";
    }

    // ECDSA SSH 공개키를 OpenSSH 포맷으로 동적 생성하는 헬퍼
    private static string GenerateOpenSshEcdsaKey(ECCurve curve, string curveName, string comment = "test@comment")
    {
        using var ecdsa = ECDsa.Create(curve);
        var parameters = ecdsa.ExportParameters(false);
        using var ms = new MemoryStream();
        
        string algo = $"ecdsa-sha2-{curveName}";
        WriteNextString(ms, algo);
        WriteNextString(ms, curveName);
        
        // Q point: 0x04 + X + Y
        byte[] x = parameters.Q.X ?? Array.Empty<byte>();
        byte[] y = parameters.Q.Y ?? Array.Empty<byte>();
        byte[] qBytes = new byte[1 + x.Length + y.Length];
        qBytes[0] = 0x04;
        Array.Copy(x, 0, qBytes, 1, x.Length);
        Array.Copy(y, 0, qBytes, 1 + x.Length, y.Length);
        
        WriteNextBytes(ms, qBytes);

        string base64 = Convert.ToBase64String(ms.ToArray());
        return $"{algo} {base64} {comment}";
    }

    private static void WriteNextString(Stream stream, string val)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(val);
        WriteNextBytes(stream, bytes);
    }

    private static void WriteNextBytes(Stream stream, byte[] bytes)
    {
        byte[] lenBytes = BitConverter.GetBytes((uint)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
        stream.Write(lenBytes, 0, 4);
        stream.Write(bytes, 0, bytes.Length);
    }

    [Fact]
    public void ParseAndValidatePublicKey_Rsa3072OrHigher_ShouldSucceed()
    {
        // Arrange
        string rsa3072 = GenerateOpenSshRsaKey(3072, "rsa-3072-key");

        // Act
        var (algorithm, keySize, comment) = SshKeyParser.ParseAndValidatePublicKey(rsa3072);

        // Assert
        Assert.Equal("ssh-rsa", algorithm);
        Assert.Equal(3072, keySize);
        Assert.Equal("rsa-3072-key", comment);
    }

    [Fact]
    public void ParseAndValidatePublicKey_Rsa2048_ShouldThrowInvalidOperationException()
    {
        // Arrange
        string rsa2048 = GenerateOpenSshRsaKey(2048, "rsa-2048-key");

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => 
            SshKeyParser.ParseAndValidatePublicKey(rsa2048));
        Assert.Contains("RSA 키는 최소 3072비트 이상", ex.Message);
    }

    [Fact]
    public void ParseAndValidatePublicKey_Ed25519_ShouldSucceed()
    {
        // Arrange
        // 유효한 Ed25519 공개키 샘플 (주석 포함)
        string ed25519Key = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIOm3I1n128eDFOm7bpfA6Sqyi8mtj0EHR4LN3Q80dQ55 my-ed25519-key";

        // Act
        var (algorithm, keySize, comment) = SshKeyParser.ParseAndValidatePublicKey(ed25519Key);

        // Assert
        Assert.Equal("ssh-ed25519", algorithm);
        Assert.Equal(256, keySize);
        Assert.Equal("my-ed25519-key", comment);
    }

    [Fact]
    public void ParseAndValidatePublicKey_EcdsaCurves_ShouldSucceed()
    {
        // Arrange
        string ecdsaP256 = GenerateOpenSshEcdsaKey(ECCurve.NamedCurves.nistP256, "nistp256", "ecdsa-p256");
        string ecdsaP384 = GenerateOpenSshEcdsaKey(ECCurve.NamedCurves.nistP384, "nistp384", "ecdsa-p384");
        string ecdsaP521 = GenerateOpenSshEcdsaKey(ECCurve.NamedCurves.nistP521, "nistp521", "ecdsa-p521");

        // Act & Assert
        var (algo256, size256, comment256) = SshKeyParser.ParseAndValidatePublicKey(ecdsaP256);
        Assert.Equal("ecdsa-sha2-nistp256", algo256);
        Assert.Equal(256, size256);
        Assert.Equal("ecdsa-p256", comment256);

        var (algo384, size384, comment384) = SshKeyParser.ParseAndValidatePublicKey(ecdsaP384);
        Assert.Equal("ecdsa-sha2-nistp384", algo384);
        Assert.Equal(384, size384);
        Assert.Equal("ecdsa-p384", comment384);

        var (algo521, size521, comment521) = SshKeyParser.ParseAndValidatePublicKey(ecdsaP521);
        Assert.Equal("ecdsa-sha2-nistp521", algo521);
        Assert.Equal(521, size521);
        Assert.Equal("ecdsa-p521", comment521);
    }

    [Fact]
    public void ParseAndValidatePublicKey_InvalidFormat_ShouldThrowArgumentException()
    {
        // Arrange
        string invalidKey = "invalid-ssh-key-without-spaces";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SshKeyParser.ParseAndValidatePublicKey(invalidKey));
    }

    [Fact]
    public void ParseAndValidatePublicKey_UnsupportedAlgorithm_ShouldThrowNotSupportedException()
    {
        // Arrange
        // ssh-dss는 지원하지 않음, base64 부분은 유효해야 함
        string dssKey = "ssh-dss AAAAB3NzaC1kc3MAAACBAP1/ dss-key";

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => SshKeyParser.ParseAndValidatePublicKey(dssKey));
    }

    [Fact]
    public void CalculateSha256Fingerprint_ShouldCalculateCorrectly()
    {
        // Arrange
        string ed25519Key = "ssh-ed25519 AAAAC3NzaC1lZDI1NTE5AAAAIOm3I1n128eDFOm7bpfA6Sqyi8mtj0EHR4LN3Q80dQ55 comment";
        
        // Act
        string fingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(ed25519Key);

        // Assert
        Assert.StartsWith("SHA256:", fingerprint);
        Assert.DoesNotContain("=", fingerprint); // 패딩이 제거되었는지 확인
    }
}
