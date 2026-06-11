using Aristokeides.Api.Components.Pages;
using Aristokeides.Api.Services;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aristokeides.Tests;

public class SetupTests : BunitTestBase
{
    [Fact]
    public void Setup_InitialRender_ShowsSqliteProviderByDefault()
    {
        // Act
        var cut = Render<Setup>();

        // Assert
        var select = cut.Find("select.form-select");
        Assert.Equal("SQLite", select.GetAttribute("value"));
        
        // SQLite 경로 인풋 확인
        var sqliteInput = cut.Find("input"); // 첫 번째 input (일반적으로 SQLite 파일 경로)
        Assert.Contains("aristokeides.db", sqliteInput.GetAttribute("value"));
    }

    [Fact]
    public void Setup_ProviderChangeToPostgres_UpdatesDbPortAndHidesSqliteFields()
    {
        // Arrange
        var cut = Render<Setup>();

        // Act - PostgreSQL로 변경
        var select = cut.Find("select.form-select");
        select.Change("PostgreSQL");

        // Assert
        // SQLite 경로 인풋이 사라지고, DB Host 인풋 등이 나타나야 함
        var markup = cut.Markup;
        Assert.Contains("Host", markup);
        Assert.Contains("5432", markup); // 기본 포트 5432가 렌더링 내에 바인딩되었는지 확인
    }

    [Fact]
    public void Setup_SubmitEmptyForm_ShowsValidationErrors()
    {
        // Arrange
        var cut = Render<Setup>();

        // Act - 폼 제출
        var form = cut.Find("form");
        form.Submit();

        // Assert
        // 관리자 아이디, 이메일, 패스워드는 [Required]이므로 에러 라벨 노출 확인
        var markup = cut.Markup;
        Assert.Contains("validation-message", markup);
    }

    [Fact]
    public void Setup_InstallSuccess_RedirectsToHomeWithForceLoad()
    {
        // Arrange
        var cut = Render<Setup>();
        var nav = Services.GetRequiredService<NavigationManager>();

        // Act - 순서대로 5개 입력란 채우기 (매 입력 후 DOM 재조회하여 리렌더링 이벤트 핸들러 ID 만료 우회)
        cut.FindAll("input")[0].Change("custom.db");
        cut.FindAll("input")[1].Change("admin");
        cut.FindAll("input")[2].Change("admin@example.com");
        cut.FindAll("input")[3].Change("Password123!");
        cut.FindAll("input")[4].Change("Password123!");

        // 폼 제출
        cut.Find("form").Submit();

        // Assert
        var fakeSetup = (FakeSetupService)Services.GetRequiredService<SetupService>();
        Assert.True(fakeSetup.InstallCalled);
        Assert.Equal("admin", fakeSetup.InstalledModel?.AdminUsername);
        
        // Navigation 검증: "/"로 이동했는지 확인
        Assert.Equal("http://localhost/", nav.Uri);
    }
}
