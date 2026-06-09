using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aristokeides.Tests;

public class GitPermissionTests
{
    private DbContextOptions<AppDbContext> CreateNewInMemoryDatabaseOptions()
    {
        return new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task CheckPermissions_OrgOwner_HasAdminAccess()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        using var db = new AppDbContext(options);

        var userId = 1;
        var orgId = 10;

        var repo = new Repository
        {
            Id = Guid.NewGuid(),
            Name = "org-private-repo",
            IsPrivate = true,
            OrganizationId = orgId,
            Status = "Ready"
        };
        db.Repositories.Add(repo);

        var orgMember = new OrganizationMember
        {
            Id = 1,
            OrganizationId = orgId,
            UserId = userId,
            Role = "Owner"
        };
        db.OrganizationMembers.Add(orgMember);
        await db.SaveChangesAsync();

        // Act & Assert (Logic simulation identical to Ssh and Http checks)
        var checkedRepo = await db.Repositories.Include(r => r.Owner).Include(r => r.Organization).FirstOrDefaultAsync(r => r.Id == repo.Id);
        Assert.NotNull(checkedRepo);

        string? maxAccess = null;
        if (checkedRepo.OwnerId == userId)
        {
            maxAccess = "Admin";
        }
        else if (checkedRepo.OrganizationId.HasValue)
        {
            bool isOrgOwner = await db.OrganizationMembers.AnyAsync(om => 
                om.OrganizationId == checkedRepo.OrganizationId.Value && 
                om.UserId == userId && 
                om.Role == "Owner");

            if (isOrgOwner)
            {
                maxAccess = "Admin";
            }
        }

        Assert.Equal("Admin", maxAccess);
    }

    [Fact]
    public async Task CheckPermissions_UserWithDirectPermission_HasAssignedAccess()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        using var db = new AppDbContext(options);

        var userId = 2;
        var orgId = 10;
        var repoId = Guid.NewGuid();

        var repo = new Repository
        {
            Id = repoId,
            Name = "org-private-repo",
            IsPrivate = true,
            OrganizationId = orgId,
            Status = "Ready"
        };
        db.Repositories.Add(repo);

        // Assign Read permission directly
        var directPermission = new RepositoryPermission
        {
            RepositoryId = repoId,
            UserId = userId,
            AccessLevel = "Read"
        };
        db.RepositoryPermissions.Add(directPermission);
        await db.SaveChangesAsync();

        // Act (Perform permission evaluation logic)
        var checkedRepo = await db.Repositories.Include(r => r.Owner).Include(r => r.Organization).FirstOrDefaultAsync(r => r.Id == repo.Id);
        Assert.NotNull(checkedRepo);

        string? maxAccess = null;
        if (checkedRepo.OwnerId == userId)
        {
            maxAccess = "Admin";
        }
        else if (checkedRepo.OrganizationId.HasValue)
        {
            bool isOrgOwner = await db.OrganizationMembers.AnyAsync(om => 
                om.OrganizationId == checkedRepo.OrganizationId.Value && 
                om.UserId == userId && 
                om.Role == "Owner");

            if (isOrgOwner)
            {
                maxAccess = "Admin";
            }
            else
            {
                var teamIds = await db.TeamMembers
                    .Where(tm => tm.UserId == userId && tm.Team.OrganizationId == checkedRepo.OrganizationId.Value)
                    .Select(tm => tm.TeamId)
                    .ToListAsync();

                var permissions = await db.RepositoryPermissions
                    .Where(rp => rp.RepositoryId == checkedRepo.Id && 
                                 (rp.UserId == userId || (rp.TeamId != null && teamIds.Contains(rp.TeamId.Value))))
                    .Select(rp => rp.AccessLevel)
                    .ToListAsync();

                if (permissions.Any())
                {
                    if (permissions.Contains("Admin")) maxAccess = "Admin";
                    else if (permissions.Contains("Write")) maxAccess = "Write";
                    else if (permissions.Contains("Read")) maxAccess = "Read";
                }
            }
        }

        // Assert
        Assert.Equal("Read", maxAccess);
    }

    [Fact]
    public async Task CheckPermissions_TeamMember_InheritsTeamPermissions()
    {
        // Arrange
        var options = CreateNewInMemoryDatabaseOptions();
        using var db = new AppDbContext(options);

        var userId = 3;
        var orgId = 10;
        var teamId = 100;
        var repoId = Guid.NewGuid();

        var repo = new Repository
        {
            Id = repoId,
            Name = "org-private-repo-team",
            IsPrivate = true,
            OrganizationId = orgId,
            Status = "Ready"
        };
        db.Repositories.Add(repo);

        var team = new Team
        {
            Id = teamId,
            OrganizationId = orgId,
            Name = "Developer Team"
        };
        db.Teams.Add(team);

        var teamMember = new TeamMember
        {
            Id = 1,
            TeamId = teamId,
            UserId = userId
        };
        db.TeamMembers.Add(teamMember);

        // Assign Write permission to the Team
        var teamPermission = new RepositoryPermission
        {
            RepositoryId = repoId,
            TeamId = teamId,
            AccessLevel = "Write"
        };
        db.RepositoryPermissions.Add(teamPermission);
        await db.SaveChangesAsync();

        // Act (Perform permission evaluation logic)
        var checkedRepo = await db.Repositories.Include(r => r.Owner).Include(r => r.Organization).FirstOrDefaultAsync(r => r.Id == repo.Id);
        Assert.NotNull(checkedRepo);

        string? maxAccess = null;
        if (checkedRepo.OwnerId == userId)
        {
            maxAccess = "Admin";
        }
        else if (checkedRepo.OrganizationId.HasValue)
        {
            bool isOrgOwner = await db.OrganizationMembers.AnyAsync(om => 
                om.OrganizationId == checkedRepo.OrganizationId.Value && 
                om.UserId == userId && 
                om.Role == "Owner");

            if (isOrgOwner)
            {
                maxAccess = "Admin";
            }
            else
            {
                var teamIds = await db.TeamMembers
                    .Where(tm => tm.UserId == userId && tm.Team.OrganizationId == checkedRepo.OrganizationId.Value)
                    .Select(tm => tm.TeamId)
                    .ToListAsync();

                var permissions = await db.RepositoryPermissions
                    .Where(rp => rp.RepositoryId == checkedRepo.Id && 
                                 (rp.UserId == userId || (rp.TeamId != null && teamIds.Contains(rp.TeamId.Value))))
                    .Select(rp => rp.AccessLevel)
                    .ToListAsync();

                if (permissions.Any())
                {
                    if (permissions.Contains("Admin")) maxAccess = "Admin";
                    else if (permissions.Contains("Write")) maxAccess = "Write";
                    else if (permissions.Contains("Read")) maxAccess = "Read";
                }
            }
        }

        // Assert
        Assert.Equal("Write", maxAccess);
    }
}
