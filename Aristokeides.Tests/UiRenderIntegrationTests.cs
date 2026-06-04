using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Aristokeides.Api.Components.Pages;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aristokeides.Tests;

public class UiRenderIntegrationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly GitBrowserService _gitService;
    private readonly string _tempRepoDir;

    public UiRenderIntegrationTests()
    {
        _tempRepoDir = Path.Combine(Path.GetTempPath(), $"ui_test_repo_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempRepoDir);
        LibGit2Sharp.Repository.Init(_tempRepoDir, isBare: false);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Aristokeides_UI_Test_{Guid.NewGuid()}")
            .Options;

        _db = new AppDbContext(options);

        var configData = new Dictionary<string, string?>
        {
            { "GitSettings:BasePath", Path.GetDirectoryName(_tempRepoDir) }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

        _gitService = new GitBrowserService(_db, config);
    }

    [Fact]
    public async Task Test_GetCommitsAsync_Returns_Verified_Metadata_To_UI()
    {
        // Arrange
        var user = new User
        {
            Username = "uitester",
            Email = "uitester@example.com",
            PasswordHash = "hash",
            Role = "Contributor"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var repoEntity = new Repository
        {
            Id = Guid.NewGuid(),
            Name = Path.GetFileNameWithoutExtension(_tempRepoDir),
            OwnerId = user.Id,
            Status = "Active"
        };
        _db.Repositories.Add(repoEntity);
        await _db.SaveChangesAsync();

        // 임시 커밋 생성
        using (var gitRepo = new LibGit2Sharp.Repository(_tempRepoDir))
        {
            var signature = new LibGit2Sharp.Signature("UI Tester", "tester@example.com", DateTimeOffset.Now);
            gitRepo.Commit("Initial Commit", signature, signature, new LibGit2Sharp.CommitOptions { AllowEmptyCommit = true });
        }

        string commitHash;
        using (var gitRepo = new LibGit2Sharp.Repository(_tempRepoDir))
        {
            commitHash = gitRepo.Head.Tip.Sha;
        }

        // DB에 Verified 서명 정보 저장
        var sigRecord = new CommitSignature
        {
            RepositoryId = repoEntity.Id,
            CommitHash = commitHash,
            Status = "Verified",
            SignerUserId = user.Id,
            Algorithm = "ssh-ed25519",
            KeyFingerprint = "SHA256:dummyfingerprint"
        };
        _db.CommitSignatures.Add(sigRecord);
        await _db.SaveChangesAsync();

        // Act
        var result = await _gitService.GetCommitsAsync(_tempRepoDir, "master", 1, 10);

        // Assert
        Assert.Single(result.Commits);
        var commitInfo = result.Commits.First();
        Assert.Equal(commitHash, commitInfo.Hash);
        Assert.Equal("Verified", commitInfo.SignatureStatus);
        Assert.Equal("SHA256:dummyfingerprint", commitInfo.SignatureFingerprint);
        Assert.Equal(user.Username, commitInfo.SignerUsername);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRepoDir))
            {
                Directory.Delete(_tempRepoDir, true);
            }
        }
        catch { }
    }
}
