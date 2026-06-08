using System.Security.Claims;
using Aristokeides.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Aristokeides.Tests;

public class RootControllerTests
{
    [Fact]
    public void Index_RedirectsToHome_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var controller = new RootController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = controller.Index();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/home", redirectResult.Url);
    }

    [Fact]
    public void Index_RedirectsToDashboard_WhenUserIsAuthenticated()
    {
        // Arrange
        var controller = new RootController();
        var httpContext = new DefaultHttpContext();
        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "Cookies"); // Setting authenticationType makes IsAuthenticated = true
        httpContext.User = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        // Act
        var result = controller.Index();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/dashboard", redirectResult.Url);
    }
}
