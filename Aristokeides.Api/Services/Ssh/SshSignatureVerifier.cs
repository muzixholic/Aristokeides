using System;
using System.Diagnostics;
using System.IO;

namespace Aristokeides.Api.Services.Ssh;

/// <summary>
/// ssh-keygen 명령어를 사용하여 SSH 커밋 서명을 검증하는 검증기.
/// </summary>
public static class SshSignatureVerifier
{
    public static bool Verify(byte[] payloadBytes, string signatureText, string publicKey)
    {
        if (payloadBytes == null || payloadBytes.Length == 0)
            throw new ArgumentException("페이로드가 비어 있습니다.");
        if (string.IsNullOrWhiteSpace(signatureText))
            throw new ArgumentException("서명 내용이 비어 있습니다.");
        if (string.IsNullOrWhiteSpace(publicKey))
            throw new ArgumentException("공개키가 비어 있습니다.");

        // 공개키에서 주석 부분 제거 (알고리즘과 키 내용만 추출)
        var parts = publicKey.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            throw new ArgumentException("유효하지 않은 공개키 포맷입니다.");
        }
        string cleanPublicKey = $"{parts[0]} {parts[1]}";

        string tempDir = Path.GetTempPath();
        string guid = Guid.NewGuid().ToString();

        string allowedSignersPath = Path.Combine(tempDir, $"allowed_signers_{guid}.tmp");
        string sigPath = Path.Combine(tempDir, $"sig_{guid}.tmp");

        try
        {
            // allowed_signers 작성
            // allowed_signers 형식: <identity> <key-type> <base64-key>
            string allowedSignersContent = $"principal {cleanPublicKey}{Environment.NewLine}";
            File.WriteAllText(allowedSignersPath, allowedSignersContent);

            // signature 작성
            File.WriteAllText(sigPath, signatureText);

            // ssh-keygen 실행
            // ssh-keygen -Y verify -f [allowed_signers] -I principal -n git -s [sig_path] < [payload_path]
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "ssh-keygen",
                    Arguments = $"-Y verify -f \"{allowedSignersPath}\" -I principal -n git -s \"{sigPath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();

                // payload를 standard input으로 직접 라이팅
                using (var stdin = process.StandardInput.BaseStream)
                {
                    stdin.Write(payloadBytes, 0, payloadBytes.Length);
                    stdin.Flush();
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    Console.WriteLine($"ssh-keygen verification failed. ExitCode: {process.ExitCode}");
                    Console.WriteLine($"Stdout: {stdout}");
                    Console.WriteLine($"Stderr: {stderr}");
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during ssh-keygen verification: {ex.Message}");
            return false;
        }
        finally
        {
            TryDeleteFile(allowedSignersPath);
            TryDeleteFile(sigPath);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // 무시
        }
    }
}
