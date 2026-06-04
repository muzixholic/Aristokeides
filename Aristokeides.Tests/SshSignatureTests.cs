using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Aristokeides.Api.Services.Ssh;
using Xunit;

namespace Aristokeides.Tests;

public class SshSignatureTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;
    private readonly string _payloadPath;
    private readonly string _sigPath;
    private readonly byte[] _payloadBytes;
    private string _publicKeyContent = "";
    private string _signatureText = "";

    public SshSignatureTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ssh_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _privateKeyPath = Path.Combine(_tempDir, "id_ed25519_test");
        _publicKeyPath = _privateKeyPath + ".pub";
        _payloadPath = Path.Combine(_tempDir, "payload.txt");
        _sigPath = _payloadPath + ".sig";

        _payloadBytes = Encoding.UTF8.GetBytes("Test Payload for SSH Signature Verification");

        GenerateTestKeyAndSignature();
    }

    private void GenerateTestKeyAndSignature()
    {
        // 1. 키 생성
        // ssh-keygen -t ed25519 -f [key_path] -N ""
        using (var process = Process.Start(new ProcessStartInfo
        {
            FileName = "ssh-keygen",
            Arguments = $"-t ed25519 -f \"{_privateKeyPath}\" -N \"\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            process?.WaitForExit();
        }

        if (!File.Exists(_privateKeyPath) || !File.Exists(_publicKeyPath))
        {
            throw new InvalidOperationException("Failed to generate test SSH key.");
        }

        _publicKeyContent = File.ReadAllText(_publicKeyPath).Trim();

        // 2. 페이로드 파일 작성
        File.WriteAllBytes(_payloadPath, _payloadBytes);

        // 3. 서명 생성
        // ssh-keygen -Y sign -f [private_key] -n git [payload_path]
        using (var process = Process.Start(new ProcessStartInfo
        {
            FileName = "ssh-keygen",
            Arguments = $"-Y sign -f \"{_privateKeyPath}\" -n git \"{_payloadPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            process?.WaitForExit();
        }

        if (!File.Exists(_sigPath))
        {
            throw new InvalidOperationException("Failed to generate SSH signature.");
        }

        _signatureText = File.ReadAllText(_sigPath).Trim();
    }

    [Fact]
    public void Test_Parser_Extracts_Correct_Info()
    {
        // Act
        var (pkBytes, algorithm, fingerprint) = SshSignatureParser.ParseSignature(_signatureText);

        // Assert
        Assert.Equal("ssh-ed25519", algorithm);
        Assert.NotNull(pkBytes);
        Assert.True(pkBytes.Length > 0);
        Assert.StartsWith("SHA256:", fingerprint);

        // 지문 일치성 확인
        var expectedFingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(_publicKeyContent);
        Assert.Equal(expectedFingerprint, fingerprint);
    }

    [Fact]
    public void Test_Verifier_Succeeds_With_Valid_Signature()
    {
        // Act
        bool result = SshSignatureVerifier.Verify(_payloadBytes, _signatureText, _publicKeyContent);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Test_Verifier_Fails_With_Modified_Payload()
    {
        // Arrange
        var modifiedPayload = Encoding.UTF8.GetBytes("Modified Payload content here");

        // Act
        bool result = SshSignatureVerifier.Verify(modifiedPayload, _signatureText, _publicKeyContent);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Test_Parser_Throws_On_Invalid_Header()
    {
        // Arrange
        byte[] dummyBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        string base64 = Convert.ToBase64String(dummyBytes);
        string invalidSig = $"-----BEGIN SSH SIGNATURE-----\n{base64}\n-----END SSH SIGNATURE-----";

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => SshSignatureParser.ParseSignature(invalidSig));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // 무시
        }
    }
}
