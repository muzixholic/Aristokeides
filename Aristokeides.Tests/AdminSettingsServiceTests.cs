using Aristokeides.Api.Models;
using Aristokeides.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace Aristokeides.Tests;

public class AdminSettingsServiceTests : IDisposable
{
    private readonly string _tempDirPath;
    private readonly string _appSettingsPath;

    public AdminSettingsServiceTests()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirPath);
        _appSettingsPath = Path.Combine(_tempDirPath, "appsettings.json");
        File.WriteAllText(_appSettingsPath, "{}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirPath))
            Directory.Delete(_tempDirPath, true);
    }

    private class FakeApplicationLifetime : IHostApplicationLifetime
    {
        public bool StopApplicationCalled { get; private set; }
        public void StopApplication() => StopApplicationCalled = true;
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
    }

    private class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string ApplicationName { get; set; } = "";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = "";

        public FakeWebHostEnvironment(string contentRootPath)
        {
            ContentRootPath = contentRootPath;
        }
    }

    [Fact]
    public void GetSettings_ReturnsValuesFromConfiguration()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?> {
            {"Database:Provider", "PostgreSQL"},
            {"Database:ConnectionString", "Host=abc"},
            {"Ssh:Port", "2222"},
            {"Ssh:Domain", "example.com"}
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var lifetime = new FakeApplicationLifetime();
        var env = new FakeWebHostEnvironment(_tempDirPath);

        var service = new AdminSettingsService(configuration, lifetime, env);

        // Act
        var settings = service.GetSettings();

        // Assert
        Assert.Equal("PostgreSQL", settings.DatabaseProvider);
        Assert.Equal("Host=abc", settings.ConnectionString);
        Assert.Equal(2222, settings.SshPort);
        Assert.Equal("example.com", settings.SshDomain);
    }

    [Fact]
    public async Task SaveSettingsAsync_UpdatesJsonFileAndStopsApp()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var lifetime = new FakeApplicationLifetime();
        var env = new FakeWebHostEnvironment(_tempDirPath);
        var service = new AdminSettingsService(configuration, lifetime, env);

        var model = new AdminSettingsViewModel
        {
            DatabaseProvider = "SQLite",
            ConnectionString = "Data Source=test.db",
            SshPort = 1234,
            SshDomain = "test.local"
        };

        // Act
        await service.SaveSettingsAsync(model, restartApp: true);

        // Assert
        Assert.True(lifetime.StopApplicationCalled);

        var json = await File.ReadAllTextAsync(_appSettingsPath);
        var doc = JsonDocument.Parse(json);
        Assert.Equal("SQLite", doc.RootElement.GetProperty("Database").GetProperty("Provider").GetString());
        Assert.Equal("Data Source=test.db", doc.RootElement.GetProperty("Database").GetProperty("ConnectionString").GetString());
        Assert.Equal(1234, doc.RootElement.GetProperty("Ssh").GetProperty("Port").GetInt32());
        Assert.Equal("test.local", doc.RootElement.GetProperty("Ssh").GetProperty("Domain").GetString());
    }
}
