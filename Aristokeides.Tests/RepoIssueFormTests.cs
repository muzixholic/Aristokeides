using System;
using System.Threading.Tasks;
using Aristokeides.Api.Components.Pages;
using Aristokeides.Api.Models;
using Bunit;
using Xunit;

namespace Aristokeides.Tests;

public class RepoIssueFormTests : BunitTestBase
{
    [Fact]
    public async Task RepoIssueForm_EmptyTitle_ShowsValidationError()
    {
        // Arrange
        // 1. 가상 데이터 셋업 (User & Repository)
        var user = new User { Id = 1, Username = "tester", Email = "tester@example.com", PasswordHash = "hash", Role = "Contributor" };
        DbContext.Users.Add(user);

        var repo = new Repository { Id = Guid.NewGuid(), Name = "test-repo", OwnerId = 1, Status = "Active", Owner = user };
        DbContext.Repositories.Add(repo);

        await DbContext.SaveChangesAsync();

        // 2. 컴포넌트 파라미터 전달 및 렌더링
        var cut = Render<RepoIssueForm>(parameters => parameters
            .Add(p => p.Username, "tester")
            .Add(p => p.RepoName, "test-repo")
        );

        // Act - 제목은 비우고 설명만 적은 뒤 제출 시도
        cut.Find("textarea#description").Change("이슈 상세 설명입니다.");
        cut.Find("form").Submit();

        // Assert
        var markup = cut.Markup;
        Assert.Contains("Title is required", markup);
    }
}
