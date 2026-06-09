using Aristokeides.Api.Models;
using Aristokeides.Api.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace Aristokeides.Tests;

public class SetupServiceTests : IDisposable
{
    private readonly string _tempDirPath;
    private readonly string _appSettingsPath;
    private readonly string _sqliteDbPath;

    public SetupServiceTests()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirPath);
        _appSettingsPath = Path.Combine(_tempDirPath, "appsettings.json");
        File.WriteAllText(_appSettingsPath, "{}");
        _sqliteDbPath = Path.Combine(_tempDirPath, "test_setup.db");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirPath))
        {
            // Give SQLite connections a moment to close
            GC.Collect();
            GC.WaitForPendingFinalizers();
            try
            {
                Directory.Delete(_tempDirPath, true);
            }
            catch { }
        }
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
    public async Task InstallAsync_WhenAlreadyInstalled_ReturnsFalse()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?> {
            {"IsInstalled", "true"}
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        var lifetime = new FakeApplicationLifetime();
        var env = new FakeWebHostEnvironment(_tempDirPath);
        var service = new SetupService(configuration, lifetime, env);

        var model = new SetupViewModel();

        // Act
        var result = await service.InstallAsync(model);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task InstallAsync_WithSqlite_RunsMigrationUpdatesSettingsAndStopsApp()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var lifetime = new FakeApplicationLifetime();
        var env = new FakeWebHostEnvironment(_tempDirPath);
        var service = new SetupService(configuration, lifetime, env);

        var model = new SetupViewModel
        {
            DatabaseProvider = "SQLite",
            SqliteFilePath = _sqliteDbPath,
            AdminUsername = "admin",
            AdminEmail = "admin@example.com",
            AdminPassword = "password123",
            AdminPasswordConfirm = "password123"
        };

        // Act
        var result = await service.InstallAsync(model);

        // Assert
        Assert.True(result);
        Assert.True(lifetime.StopApplicationCalled);
        
        // Verify appsettings.json was updated
        var json = await File.ReadAllTextAsync(_appSettingsPath);
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("IsInstalled").GetBoolean());
        Assert.Equal("SQLite", doc.RootElement.GetProperty("Database").GetProperty("Provider").GetString());
        Assert.Equal($"Data Source={_sqliteDbPath}", doc.RootElement.GetProperty("Database").GetProperty("ConnectionString").GetString());

        // Verify SQLite database was created
        Assert.True(File.Exists(_sqliteDbPath));
    }
}
