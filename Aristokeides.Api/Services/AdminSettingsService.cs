using System.Text.Json;
using System.Text.Json.Nodes;
using Aristokeides.Api.Models;

namespace Aristokeides.Api.Services;

public class AdminSettingsService
{
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly string _appSettingsPath;

    public AdminSettingsService(IConfiguration configuration, IHostApplicationLifetime appLifetime, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _appLifetime = appLifetime;
        _appSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.json");
    }

    public AdminSettingsViewModel GetSettings()
    {
        return new AdminSettingsViewModel
        {
            DatabaseProvider = _configuration["Database:Provider"] ?? "SQLite",
            ConnectionString = _configuration["Database:ConnectionString"] ?? string.Empty,
            SshPort = _configuration.GetValue<int>("Ssh:Port", 2222),
            SshDomain = _configuration["Ssh:Domain"] ?? "localhost"
        };
    }

    public async Task SaveSettingsAsync(AdminSettingsViewModel model, bool restartApp = false)
    {
        var json = await File.ReadAllTextAsync(_appSettingsPath);
        var jsonObj = JsonNode.Parse(json)!.AsObject();

        if (jsonObj["Database"] == null)
            jsonObj["Database"] = new JsonObject();
            
        jsonObj["Database"]!["Provider"] = model.DatabaseProvider;
        jsonObj["Database"]!["ConnectionString"] = model.ConnectionString;

        if (jsonObj["Ssh"] == null)
            jsonObj["Ssh"] = new JsonObject();

        jsonObj["Ssh"]!["Port"] = model.SshPort;
        jsonObj["Ssh"]!["Domain"] = model.SshDomain;

        await File.WriteAllTextAsync(_appSettingsPath, jsonObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        if (restartApp)
        {
            _appLifetime.StopApplication();
        }
    }
}
