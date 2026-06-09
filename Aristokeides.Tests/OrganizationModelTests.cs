using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aristokeides.Tests;

public class OrganizationModelTests : IDisposable
{
    private readonly AppDbContext _db;

    public OrganizationModelTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"Aristokeides_Org_Test_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    // Helper for Model Validation
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void Organization_Name_Regex_Validation()
    {
        // Arrange & Act & Assert
        var pattern = @"^[a-z0-9\-]+$";

        // Valid cases
        Assert.Matches(pattern, "my-org");
        Assert.Matches(pattern, "org123");
        Assert.Matches(pattern, "simple");

        // Invalid cases
        Assert.DoesNotMatch(pattern, "My-Org"); // uppercase
        Assert.DoesNotMatch(pattern, "my_org");  // underscore
        Assert.DoesNotMatch(pattern, "my org");  // space
        Assert.DoesNotMatch(pattern, "org@123"); // special char
    }

    [Fact]
    public async Task Create_Organization_With_Owner_Linkage()
    {
        // Arrange
        var user = new User
        {
            Email = "owner@example.com",
            Username = "owneruser",
            PasswordHash = "hash",
            Role = "Contributor"
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Act
        var org = new Organization
        {
            Name = "google-deepmind",
            Description = "Advanced AI Team",
            CreatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);

        var member = new OrganizationMember
        {
            Organization = org,
            UserId = user.Id,
            Role = "Owner",
            JoinedAt = DateTime.UtcNow
        };
        _db.OrganizationMembers.Add(member);
        await _db.SaveChangesAsync();

        // Assert
        var savedOrg = await _db.Organizations
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Name == "google-deepmind");

        Assert.NotNull(savedOrg);
        Assert.Single(savedOrg.Members);
        Assert.Equal("Owner", savedOrg.Members.First().Role);
        Assert.Equal(user.Id, savedOrg.Members.First().UserId);
    }

    [Fact]
    public async Task Organization_Name_Collision_With_Username_Check()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "clash@example.com",
            Username = "clashname",
            PasswordHash = "hash",
            Role = "Reader"
        };
        _db.Users.Add(existingUser);
        await _db.SaveChangesAsync();

        // Act & Assert: Simulate page submit validation
        var proposedOrgName = "clashname"; // same as username
        var nameLower = proposedOrgName.ToLowerInvariant();

        // Check against users
        var isUsernameDuplicate = await _db.Users.AnyAsync(u => u.Username.ToLower() == nameLower);
        Assert.True(isUsernameDuplicate);
    }

    [Fact]
    public async Task Organization_Name_Collision_With_Existing_Organization_Check()
    {
        // Arrange
        var existingOrg = new Organization
        {
            Name = "existing-org",
            Description = "Old Org"
        };
        _db.Organizations.Add(existingOrg);
        await _db.SaveChangesAsync();

        // Act & Assert
        var proposedOrgName = "existing-org";
        var nameLower = proposedOrgName.ToLowerInvariant();

        var isOrgNameDuplicate = await _db.Organizations.AnyAsync(o => o.Name.ToLower() == nameLower);
        Assert.True(isOrgNameDuplicate);
    }

    [Fact]
    public async Task Team_And_TeamMember_Flow()
    {
        // Arrange
        var org = new Organization { Name = "test-org-team" };
        _db.Organizations.Add(org);

        var user = new User { Email = "team@example.com", Username = "teammember", PasswordHash = "hash", Role = "Reader" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Act
        var team = new Team
        {
            OrganizationId = org.Id,
            Name = "dev-ops",
            Description = "Deployment team"
        };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        var teamMember = new TeamMember
        {
            TeamId = team.Id,
            UserId = user.Id
        };
        _db.TeamMembers.Add(teamMember);
        await _db.SaveChangesAsync();

        // Assert
        var savedTeam = await _db.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Name == "dev-ops" && t.OrganizationId == org.Id);

        Assert.NotNull(savedTeam);
        Assert.Single(savedTeam.Members);
        Assert.Equal(user.Id, savedTeam.Members.First().UserId);
    }

    [Fact]
    public async Task RepositoryPermission_Flow()
    {
        // Arrange
        var user = new User { Email = "perm@example.com", Username = "permuser", PasswordHash = "hash", Role = "Reader" };
        _db.Users.Add(user);

        var repo = new Repository
        {
            Id = Guid.NewGuid(),
            Name = "perm-repo",
            Owner = user,
            Status = "Created"
        };
        _db.Repositories.Add(repo);
        await _db.SaveChangesAsync();

        // Act
        var permission = new RepositoryPermission
        {
            RepositoryId = repo.Id,
            UserId = user.Id,
            AccessLevel = "Write"
        };
        _db.RepositoryPermissions.Add(permission);
        await _db.SaveChangesAsync();

        // Assert
        var savedPermission = await _db.RepositoryPermissions
            .FirstOrDefaultAsync(p => p.RepositoryId == repo.Id && p.UserId == user.Id);

        Assert.NotNull(savedPermission);
        Assert.Equal("Write", savedPermission.AccessLevel);
    }
}
