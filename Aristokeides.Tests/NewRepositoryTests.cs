using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Aristokeides.Api.Components.Pages;
using Aristokeides.Api.Models;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aristokeides.Tests;

public class NewRepositoryTests : BunitTestBase
{
    [Fact]
    public async Task NewRepository_RenderWithOrgList_ShowsOrgsInSelect()
    {
        // Arrange
        // 1. 사용자 로그인 처리 (bUnit 2.0 style AddAuthorization)
        var auth = this.AddAuthorization();
        auth.SetAuthorized("tester");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, "1"));

        // 2. 가상 조직 데이터 DB 준비
        var user = new User { Id = 1, Username = "tester", Email = "tester@example.com", PasswordHash = "hash", Role = "Contributor" };
        DbContext.Users.Add(user);

        var org = new Organization { Id = 10, Name = "testorg" };
        DbContext.Organizations.Add(org);

        var member = new OrganizationMember { Id = 100, OrganizationId = 10, UserId = 1, Role = "Owner", Organization = org, User = user };
        DbContext.OrganizationMembers.Add(member);

        await DbContext.SaveChangesAsync();

        // Act
        var cut = Render<NewRepository>();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("testorg (Organization)", markup);
    }

    [Fact]
    public async Task NewRepository_DuplicateName_ShowsErrorMessage()
    {
        // Arrange
        var auth = this.AddAuthorization();
        auth.SetAuthorized("tester");
        auth.SetClaims(new Claim(ClaimTypes.NameIdentifier, "1"));

        var user = new User { Id = 1, Username = "tester", Email = "tester@example.com", PasswordHash = "hash", Role = "Contributor" };
        DbContext.Users.Add(user);

        var repo = new Repository { Id = Guid.NewGuid(), Name = "duplicate-repo", OwnerId = 1, Status = "Active" };
        DbContext.Repositories.Add(repo);

        await DbContext.SaveChangesAsync();

        var cut = Render<NewRepository>();

        // Act - 동일한 이름 입력 후 제출
        cut.Find("input#repo-name").Change("duplicate-repo");
        cut.Find("form").Submit();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("이미 사용 중인 저장소 이름입니다: duplicate-repo", markup);
    }
}
