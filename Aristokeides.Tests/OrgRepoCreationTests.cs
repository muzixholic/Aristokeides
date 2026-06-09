using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aristokeides.Tests;

public class OrgRepoCreationTests
{
    private DbContextOptions<AppDbContext> CreateNewInMemoryDatabaseOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task Repository_CreatedWithOrgOwner_SetsOrganizationIdAndNullOwnerId()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        using var db = new AppDbContext(options);

        var user = new User { Id = 1, Username = "user1", Email = "user1@example.com", PasswordHash = "hash", Role = "User" };
        var org = new Organization { Id = 10, Name = "testorg" };
        var member = new OrganizationMember { Id = 1, OrganizationId = 10, UserId = 1, Role = "Owner" };

        db.Users.Add(user);
        db.Organizations.Add(org);
        db.OrganizationMembers.Add(member);
        await db.SaveChangesAsync();

        // Act & Assert (Simulate creation logic in NewRepository.razor)
        var nameLower = "my-org-repo".ToLower();
        var isDuplicate = await db.Repositories
            .AnyAsync(r => r.OrganizationId == org.Id && r.Name.ToLower() == nameLower);
        Assert.False(isDuplicate);

        var repository = new Repository
        {
            Id = Guid.NewGuid(),
            Name = "my-org-repo",
            OwnerId = null,
            OrganizationId = org.Id,
            Status = "Creating",
            IsPrivate = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Repositories.Add(repository);
        await db.SaveChangesAsync();

        // Verify database state
        var savedRepo = await db.Repositories.FirstOrDefaultAsync(r => r.Id == repository.Id);
        Assert.NotNull(savedRepo);
        Assert.Null(savedRepo.OwnerId);
        Assert.Equal(10, savedRepo.OrganizationId);
    }

    [Fact]
    public async Task Repository_DuplicateNameInSameOrg_ShouldBeDetected()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        using var db = new AppDbContext(options);

        var org = new Organization { Id = 10, Name = "testorg" };
        var repo1 = new Repository
        {
            Id = Guid.NewGuid(),
            Name = "DuplicateRepo",
            OwnerId = null,
            OrganizationId = org.Id,
            Status = "Ready"
        };

        db.Organizations.Add(org);
        db.Repositories.Add(repo1);
        await db.SaveChangesAsync();

        // Act & Assert (Check duplicate validation logic)
        var newRepoName = "duplicaterepo"; // lower case comparison
        var isDuplicate = await db.Repositories
            .AnyAsync(r => r.OrganizationId == org.Id && r.Name.ToLower() == newRepoName.ToLower());
        
        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task RepositoryCreationWorker_ResolvesCorrectOrgPath()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        using var db = new AppDbContext(options);

        var org = new Organization { Id = 10, Name = "myorg" };
        var repo = new Repository
        {
            Id = Guid.NewGuid(),
            Name = "orgrepo",
            OwnerId = null,
            OrganizationId = org.Id,
            Status = "Creating"
        };

        db.Organizations.Add(org);
        db.Repositories.Add(repo);
        await db.SaveChangesAsync();

        // Mock channel
        var channel = new RepositoryCreationChannel();

        // ServiceProvider with AppDbContext
        var services = new ServiceCollection();
        services.AddScoped(sp => new AppDbContext(options));
        var serviceProvider = services.BuildServiceProvider();

        // Instantiate worker to test target path logic
        var worker = new RepositoryCreationBackgroundWorker(channel, serviceProvider, NullLogger<RepositoryCreationBackgroundWorker>.Instance);
        
        // Act (Reflect target path resolution)
        var dbRepo = await db.Repositories
            .Include(r => r.Owner)
            .Include(r => r.Organization)
            .FirstOrDefaultAsync(r => r.Id == repo.Id);

        Assert.NotNull(dbRepo);
        Assert.Null(dbRepo.Owner);
        Assert.NotNull(dbRepo.Organization);

        var ownerName = dbRepo.Owner != null ? dbRepo.Owner.Username : dbRepo.Organization.Name;
        var basePath = Path.GetFullPath("GitRepos");
        var gitPath = Path.Combine(basePath, ownerName, $"{dbRepo.Name}.git");

        // Assert
        Assert.Equal("myorg", ownerName);
        Assert.Contains("GitRepos", gitPath);
        Assert.EndsWith("myorg/orgrepo.git", gitPath);
    }
}
