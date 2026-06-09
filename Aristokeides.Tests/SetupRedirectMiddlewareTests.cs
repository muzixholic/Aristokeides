using Aristokeides.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Aristokeides.Tests;

public class SetupRedirectMiddlewareTests
{
    private IConfiguration CreateConfiguration(bool isInstalled)
    {
        var inMemorySettings = new Dictionary<string, string?> {
            {"IsInstalled", isInstalled.ToString()}
        };
        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task InvokeAsync_WhenInstalled_CallsNextDelegate()
    {
        // Arrange
        var isNextCalled = false;
        var middleware = new SetupRedirectMiddleware(innerHttpContext =>
        {
            isNextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        var configuration = CreateConfiguration(true);

        // Act
        await middleware.InvokeAsync(context, configuration);

        // Assert
        Assert.True(isNextCalled);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/setup")]
    [InlineData("/_framework/blazor.server.js")]
    [InlineData("/css/app.css")]
    public async Task InvokeAsync_WhenNotInstalled_ExemptPaths_CallsNextDelegate(string path)
    {
        // Arrange
        var isNextCalled = false;
        var middleware = new SetupRedirectMiddleware(innerHttpContext =>
        {
            isNextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var configuration = CreateConfiguration(false);

        // Act
        await middleware.InvokeAsync(context, configuration);

        // Assert
        Assert.True(isNextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenNotInstalled_NonExemptPath_RedirectsToSetup()
    {
        // Arrange
        var isNextCalled = false;
        var middleware = new SetupRedirectMiddleware(innerHttpContext =>
        {
            isNextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/dashboard";
        var configuration = CreateConfiguration(false);

        // Act
        await middleware.InvokeAsync(context, configuration);

        // Assert
        Assert.False(isNextCalled);
        Assert.Equal(302, context.Response.StatusCode);
        Assert.Equal("/setup", context.Response.Headers["Location"]);
    }
}
