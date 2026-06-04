using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services.Ssh;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aristokeides.Tests;

public class PushHookIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _privateKeyPath;
    private readonly string _publicKeyPath;
    private readonly byte[] _payloadBytes;
    private readonly string _publicKeyContent;
    private readonly AppDbContext _db;
    private readonly IServiceProvider _serviceProvider;

    public PushHookIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"push_hook_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _privateKeyPath = Path.Combine(_tempDir, "id_ed25519_test");
        _publicKeyPath = _privateKeyPath + ".pub";
        _payloadBytes = Encoding.UTF8.GetBytes("Test Payload");

        // SSH 테스트 키 생성
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

        _publicKeyContent = File.ReadAllText(_publicKeyPath).Trim();

        // InMemory AppDbContext 구성
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase($"Aristokeides_Test_{Guid.NewGuid()}"));
        
        _serviceProvider = services.BuildServiceProvider();
        _db = _serviceProvider.GetRequiredService<AppDbContext>();
    }

    [Fact]
    public async Task Test_VerifyNewCommits_Correctly_Identifies_Different_Signature_Statuses()
    {
        // Arrange
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
        
        // 2. DB 기본 셋업
        var user = new User
        {
            Username = "testuser",
            Email = "testuser@example.com",
            PasswordHash = "hashed",
            Role = "Contributor"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var repoEntity = new Repository
        {
            Id = Guid.NewGuid(),
            Name = "testrepo",
            OwnerId = user.Id,
            Status = "Active"
        };
        _db.Repositories.Add(repoEntity);
        await _db.SaveChangesAsync();

        // 3. 서명 없는 커밋 생성 (NoSignature)
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "No signature");
        Process.Start(new ProcessStartInfo { FileName = "git", Arguments = $"-C \"{_tempDir}\" add .", CreateNoWindow = true })?.WaitForExit();
        Process.Start(new ProcessStartInfo { FileName = "git", Arguments = $"-C \"{_tempDir}\" commit -m \"Unsigned Commit\"", CreateNoWindow = true })?.WaitForExit();

        string unsignedCommitHash = GetHeadHash();

        // 4. 서명 설정 후 서명 커밋 생성 (이후 검증 테스트용)
        Process.Start(new ProcessStartInfo { FileName = "git", Arguments = $"-C \"{_tempDir}\" config gpg.format ssh", CreateNoWindow = true })?.WaitForExit();
        Process.Start(new ProcessStartInfo { FileName = "git", Arguments = $"-C \"{_tempDir}\" config user.signingkey \"{_privateKeyPath}\"", CreateNoWindow = true })?.WaitForExit();

        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "Signed content");
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

        string signedCommitHash = GetHeadHash();

        // 5. 검증기 서비스 생성
        var mockScopeFactory = new MockServiceScopeFactory(_serviceProvider);
        var verifierService = new SshSignatureVerificationService(mockScopeFactory);

        // --- Scenario A: SSH Key가 등록되지 않은 상태에서 검증 (Unknown 기대) ---
        await verifierService.VerifyNewCommitsAsync(_tempDir, "0000000000000000000000000000000000000000", signedCommitHash, repoEntity.Id);

        var unsignedRecord = await _db.CommitSignatures.FirstOrDefaultAsync(s => s.CommitHash == unsignedCommitHash);
        var signedUnknownRecord = await _db.CommitSignatures.FirstOrDefaultAsync(s => s.CommitHash == signedCommitHash);

        Assert.NotNull(unsignedRecord);
        Assert.Equal("NoSignature", unsignedRecord.Status);

        Assert.NotNull(signedUnknownRecord);
        Assert.Equal("Unknown", signedUnknownRecord.Status);
        Assert.Null(signedUnknownRecord.SignerUserId);

        // --- Scenario B: SSH Key가 등록된 상태에서 재검증 (Verified 기대) ---
        // 지문 연산하여 DB에 키 등록
        string fingerprint = SshFingerprintCalculator.CalculateSha256Fingerprint(_publicKeyContent);
        var sshKey = new SshKey
        {
            UserId = user.Id,
            Label = "Test Key",
            PublicKey = _publicKeyContent,
            Fingerprint = fingerprint
        };
        _db.SshKeys.Add(sshKey);
        await _db.SaveChangesAsync();

        // 다시 서명 검증을 트리거
        await verifierService.VerifyNewCommitsAsync(_tempDir, unsignedCommitHash, signedCommitHash, repoEntity.Id);

        var signedVerifiedRecord = await _db.CommitSignatures.FirstOrDefaultAsync(s => s.CommitHash == signedCommitHash);
        Assert.NotNull(signedVerifiedRecord);
        Assert.Equal("Verified", signedVerifiedRecord.Status);
        Assert.Equal(user.Id, signedVerifiedRecord.SignerUserId);
    }

    private string GetHeadHash()
    {
        using (var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"-C \"{_tempDir}\" rev-parse HEAD",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        }))
        {
            return process?.StandardOutput.ReadToEnd().Trim() ?? "";
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
        catch { }
    }
}

// IServiceScopeFactory Mocking Helpers
public class MockServiceScopeFactory : IServiceScopeFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MockServiceScopeFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IServiceScope CreateScope()
    {
        return new MockServiceScope(_serviceProvider);
    }
}

public class MockServiceScope : IServiceScope
{
    public IServiceProvider ServiceProvider { get; }

    public MockServiceScope(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    public void Dispose() { }
}
