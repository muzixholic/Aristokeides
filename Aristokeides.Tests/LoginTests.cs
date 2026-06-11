using Aristokeides.Api.Components.Pages;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aristokeides.Tests;

public class LoginTests : BunitTestBase
{
    [Fact]
    public void Login_ErrorParameter_InvalidCredentials_RendersErrorMessage()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/login?Error=invalid_credentials");

        // Act
        var cut = Render<Login>();

        // Assert
        var html = cut.Markup;
        Assert.Contains("이메일 또는 비밀번호가 올바르지 않습니다.", html);
    }

    [Fact]
    public void Login_ErrorParameter_Timeout_RendersTimeoutMessage()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/login?Error=timeout");

        // Act
        var cut = Render<Login>();

        // Assert
        var html = cut.Markup;
        Assert.Contains("인증 세션이 만료되었습니다. 다시 로그인해 주세요.", html);
    }

    [Fact]
    public void Login_RegisteredParameter_True_RendersSuccessMessage()
    {
        // Arrange
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("http://localhost/login?Registered=true");

        // Act
        var cut = Render<Login>();

        // Assert
        var html = cut.Markup;
        Assert.Contains("회원가입이 완료되었습니다. 로그인해 주세요.", html);
    }
}
