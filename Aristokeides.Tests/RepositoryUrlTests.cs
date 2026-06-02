using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Aristokeides.Api.Services.Ssh;
using Xunit;

namespace Aristokeides.Tests;

public class RepositoryUrlTests
{
    [Fact]
    public void GetSshCloneUrl_WithDefaultSettings_ShouldReturnCorrectUrl()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        var helper = new SshUrlHelper(configuration);

        // Act
        var url = helper.GetSshCloneUrl("testuser", "testrepo");

        // Assert
        Assert.Equal("ssh://git@localhost:2222/testuser/testrepo.git", url);
    }

    [Theory]
    [InlineData("example.com", 2222, "ssh://git@example.com:2222/username/repo.git")]
    [InlineData("git.mycompany.com", 22, "ssh://git@git.mycompany.com:22/username/repo.git")]
    [InlineData("127.0.0.1", 9922, "ssh://git@127.0.0.1:9922/username/repo.git")]
    public void GetSshCloneUrl_WithCustomSettings_ShouldReturnConfiguredUrl(string domain, int port, string expectedUrl)
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Ssh:Domain", domain},
            {"Ssh:Port", port.ToString()}
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var helper = new SshUrlHelper(configuration);

        // Act
        var url = helper.GetSshCloneUrl("username", "repo");

        // Assert
        Assert.Equal(expectedUrl, url);
    }

    [Fact]
    public void GetHttpCloneUrl_ShouldReturnCorrectUrl()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var helper = new SshUrlHelper(configuration);

        // Act
        var url = helper.GetHttpCloneUrl("testuser", "testrepo");

        // Assert
        Assert.Equal("http://localhost:5000/testuser/testrepo.git", url);
    }
}
