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

    [Fact]
    public void Test_Git_CatFile_Extracts_Correctly()
    {
        // 1. 임시 git 저장소 생성
        using (var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C \"{_tempDir}\" init",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            process?.WaitForExit();
        }

        Process.Start(new ProcessStartInfo { FileName = "git", Arguments = $"-C \"{_tempDir}\" config user.name \"Test User\"", CreateNoWindow = true })?.WaitForExit();
        Process.Start(new ProcessStartInfo { FileName = "git", Arguments = $"-C \"{_tempDir}\" config user.email \"test@example.com\"", CreateNoWindow = true })?.WaitForExit();
        Process.Start(new ProcessStartInfo { FileName = "git", Arguments = $"-C \"{_tempDir}\" config gpg.format ssh", CreateNoWindow = true })?.WaitForExit();
        Process.Start(new ProcessStartInfo { FileName = "git", Arguments = $"-C \"{_tempDir}\" config user.signingkey \"{_privateKeyPath}\"", CreateNoWindow = true })?.WaitForExit();

        // 2. 파일 추가 및 첫 커밋 생성
        File.WriteAllText(Path.Combine(_tempDir, "file.txt"), "Hello");
        Process.Start(new ProcessStartInfo { FileName = "git", Arguments = $"-C \"{_tempDir}\" add .", CreateNoWindow = true })?.WaitForExit();
        
        using (var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C \"{_tempDir}\" commit -S -m \"Signed Commit\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            process?.WaitForExit();
        }

        // 커밋 해시 구하기
        string commitHash;
        using (var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C \"{_tempDir}\" rev-parse HEAD",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            commitHash = process?.StandardOutput.ReadToEnd().Trim() ?? "";
        }

        // Act
        var (payloadBytes, signature) = ExtractSignatureAndPayload(_tempDir, commitHash);

        // Assert
        Assert.NotNull(signature);
        Assert.Contains("-----BEGIN SSH SIGNATURE-----", signature);
        Assert.Contains("-----END SSH SIGNATURE-----", signature);

        // 검증도 통과하는지 확인
        bool isVerified = SshSignatureVerifier.Verify(payloadBytes, signature, _publicKeyContent);
        Assert.True(isVerified);
    }

    private (byte[] payloadBytes, string? signatureText) ExtractSignatureAndPayload(string repoPath, string commitHash)
    {
        using (var process = new Process())
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"-C \"{repoPath}\" cat-file commit {commitHash}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Failed to retrieve commit raw data: {process.StandardError.ReadToEnd()}");
            }

            var lines = output.Split(new[] { "\n" }, StringSplitOptions.None);
            var signatureSb = new StringBuilder();
            var payloadLines = new List<string>();
            bool insideSignature = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedEndLine = line.EndsWith("\r") ? line.Substring(0, line.Length - 1) : line;

                if (trimmedEndLine.StartsWith("gpgsig "))
                {
                    insideSignature = true;
                    signatureSb.AppendLine(trimmedEndLine.Substring(7));
                    continue;
                }

                if (insideSignature)
                {
                    if (trimmedEndLine.StartsWith(" "))
                    {
                        signatureSb.AppendLine(trimmedEndLine.Substring(1));
                        continue;
                    }
                    else
                    {
                        insideSignature = false;
                    }
                }

                payloadLines.Add(line);
            }

            string? signature = signatureSb.Length > 0 ? signatureSb.ToString().Trim() : null;
            string payloadText = string.Join("\n", payloadLines);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadText);

            return (payloadBytes, signature);
        }
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
