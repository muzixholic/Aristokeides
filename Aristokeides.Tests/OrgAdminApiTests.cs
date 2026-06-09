using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aristokeides.Tests;

public class OrgAdminApiTests : IDisposable
{
    private readonly AppDbContext _db;

    public OrgAdminApiTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"Aristokeides_OrgAdmin_Test_{Guid.NewGuid()}")
            .Options;
        _db = new AppDbContext(options);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<(User owner, Organization org)> CreateTestOrgAsync(string orgName)
    {
        var owner = new User
        {
            Email = $"{orgName}-owner@example.com",
            Username = $"{orgName}owner",
            PasswordHash = "hash",
            Role = "Contributor"
        };
        _db.Users.Add(owner);
        await _db.SaveChangesAsync();

        var org = new Organization
        {
            Name = orgName,
            Description = "Test organization",
            CreatedAt = DateTime.UtcNow
        };
        _db.Organizations.Add(org);

        var member = new OrganizationMember
        {
            Organization = org,
            UserId = owner.Id,
            Role = "Owner",
            JoinedAt = DateTime.UtcNow
        };
        _db.OrganizationMembers.Add(member);
        await _db.SaveChangesAsync();

        return (owner, org);
    }

    [Fact]
    public async Task Invite_Member_Success_And_Duplicate_Check()
    {
        // Arrange
        var (owner, org) = await CreateTestOrgAsync("acme-corp");
        var newUser = new User
        {
            Email = "member@example.com",
            Username = "newmember",
            PasswordHash = "hash",
            Role = "Reader"
        };
        _db.Users.Add(newUser);
        await _db.SaveChangesAsync();

        // Act & Assert 1: 초대 성공 케이스
        var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == "newmember");
        Assert.NotNull(targetUser);

        var isAlreadyMember = await _db.OrganizationMembers.AnyAsync(om => om.OrganizationId == org.Id && om.UserId == targetUser.Id);
        Assert.False(isAlreadyMember);

        var newMember = new OrganizationMember
        {
            OrganizationId = org.Id,
            UserId = targetUser.Id,
            Role = "Member",
            JoinedAt = DateTime.UtcNow
        };
        _db.OrganizationMembers.Add(newMember);
        await _db.SaveChangesAsync();

        // 초대 완료 확인
        var members = await _db.OrganizationMembers.Where(om => om.OrganizationId == org.Id).ToListAsync();
        Assert.Equal(2, members.Count);
        Assert.Contains(members, m => m.UserId == targetUser.Id && m.Role == "Member");

        // Act & Assert 2: 중복 초대 방지 체크
        var checkDuplicate = await _db.OrganizationMembers.AnyAsync(om => om.OrganizationId == org.Id && om.UserId == targetUser.Id);
        Assert.True(checkDuplicate);
    }

    [Fact]
    public async Task Change_Member_Role_Success()
    {
        // Arrange
        var (owner, org) = await CreateTestOrgAsync("role-org");
        var user = new User { Email = "m1@example.com", Username = "member1", PasswordHash = "hash", Role = "Reader" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var member = new OrganizationMember { OrganizationId = org.Id, UserId = user.Id, Role = "Member" };
        _db.OrganizationMembers.Add(member);
        await _db.SaveChangesAsync();

        // Act: Role을 Owner로 업데이트
        var dbMember = await _db.OrganizationMembers.FirstOrDefaultAsync(om => om.Id == member.Id);
        Assert.NotNull(dbMember);
        dbMember.Role = "Owner";
        await _db.SaveChangesAsync();

        // Assert
        var updatedMember = await _db.OrganizationMembers.FindAsync(member.Id);
        Assert.NotNull(updatedMember);
        Assert.Equal("Owner", updatedMember.Role);
    }

    [Fact]
    public async Task Revoke_Membership_Cleans_Up_Team_And_Permissions()
    {
        // Arrange
        var (owner, org) = await CreateTestOrgAsync("cleanup-org");
        var user = new User { Email = "m2@example.com", Username = "member2", PasswordHash = "hash", Role = "Reader" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // 1. 조직 가입
        var orgMember = new OrganizationMember { OrganizationId = org.Id, UserId = user.Id, Role = "Member" };
        _db.OrganizationMembers.Add(orgMember);

        // 2. 팀 생성 및 팀원 매핑
        var team = new Team { OrganizationId = org.Id, Name = "devs" };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        var teamMember = new TeamMember { TeamId = team.Id, UserId = user.Id };
        _db.TeamMembers.Add(teamMember);

        // 3. 저장소 생성 및 개별 권한 부여
        var repo = new Repository { Id = Guid.NewGuid(), Name = "org-repo", OrganizationId = org.Id, Status = "Created" };
        _db.Repositories.Add(repo);
        await _db.SaveChangesAsync();

        var perm = new RepositoryPermission { RepositoryId = repo.Id, UserId = user.Id, AccessLevel = "Read" };
        _db.RepositoryPermissions.Add(perm);
        await _db.SaveChangesAsync();

        // Act: 방출 시나리오 수행 (OrgSettings.razor 비즈니스 로직 시뮬레이션)
        var dbMember = await _db.OrganizationMembers.FirstOrDefaultAsync(om => om.Id == orgMember.Id);
        Assert.NotNull(dbMember);

        // 1. 해당 조직 아래 모든 팀에서 매핑 제거
        var teamMemberships = await _db.TeamMembers
            .Where(tm => tm.UserId == user.Id && tm.Team.OrganizationId == org.Id)
            .ToListAsync();
        _db.TeamMembers.RemoveRange(teamMemberships);

        // 2. 해당 조직 아래 모든 저장소의 직접 사용자 권한 제거
        var userPermissions = await _db.RepositoryPermissions
            .Where(rp => rp.UserId == user.Id && rp.Repository.OrganizationId == org.Id)
            .ToListAsync();
        _db.RepositoryPermissions.RemoveRange(userPermissions);

        // 3. 조직원 삭제
        _db.OrganizationMembers.Remove(dbMember);
        await _db.SaveChangesAsync();

        // Assert: 모두 제거되었는지 검증
        var isStillOrgMember = await _db.OrganizationMembers.AnyAsync(om => om.OrganizationId == org.Id && om.UserId == user.Id);
        Assert.False(isStillOrgMember);

        var isStillTeamMember = await _db.TeamMembers.AnyAsync(tm => tm.TeamId == team.Id && tm.UserId == user.Id);
        Assert.False(isStillTeamMember);

        var hasStillPerm = await _db.RepositoryPermissions.AnyAsync(rp => rp.RepositoryId == repo.Id && rp.UserId == user.Id);
        Assert.False(hasStillPerm);
    }

    [Fact]
    public async Task Team_Creation_Uniqueness_Validation()
    {
        // Arrange
        var (owner, org) = await CreateTestOrgAsync("team-org");
        var team1 = new Team { OrganizationId = org.Id, Name = "qa-team" };
        _db.Teams.Add(team1);
        await _db.SaveChangesAsync();

        // Act: 동일 이름의 팀 생성 시도
        var proposedTeamName = "qa-team";
        var isDuplicate = await _db.Teams.AnyAsync(t => 
            t.OrganizationId == org.Id && 
            t.Name.ToLower() == proposedTeamName.ToLower());

        // Assert
        Assert.True(isDuplicate);
    }

    [Fact]
    public async Task Add_And_Remove_Team_Repository_Permission()
    {
        // Arrange
        var (owner, org) = await CreateTestOrgAsync("perm-org");
        var team = new Team { OrganizationId = org.Id, Name = "design-team" };
        _db.Teams.Add(team);

        var repo = new Repository { Id = Guid.NewGuid(), Name = "design-assets", OrganizationId = org.Id, Status = "Created" };
        _db.Repositories.Add(repo);
        await _db.SaveChangesAsync();

        // Act 1: 팀 저장소 권한 추가
        var perm = new RepositoryPermission
        {
            RepositoryId = repo.Id,
            TeamId = team.Id,
            AccessLevel = "Write"
        };
        _db.RepositoryPermissions.Add(perm);
        await _db.SaveChangesAsync();

        // Assert 1
        var savedPerm = await _db.RepositoryPermissions
            .FirstOrDefaultAsync(rp => rp.RepositoryId == repo.Id && rp.TeamId == team.Id);
        Assert.NotNull(savedPerm);
        Assert.Equal("Write", savedPerm.AccessLevel);

        // Act 2: 권한 취소
        _db.RepositoryPermissions.Remove(savedPerm);
        await _db.SaveChangesAsync();

        // Assert 2
        var checkDeleted = await _db.RepositoryPermissions
            .AnyAsync(rp => rp.RepositoryId == repo.Id && rp.TeamId == team.Id);
        Assert.False(checkDeleted);
    }
}
