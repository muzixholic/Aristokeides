using System.Text.Json;
using System.Text.Json.Nodes;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.EntityFrameworkCore;
using BC = BCrypt.Net.BCrypt;

namespace Aristokeides.Api.Services;

public class SetupService
{
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _appLifetime;

    private readonly IWebHostEnvironment _env;

    public SetupService(IConfiguration configuration, IHostApplicationLifetime appLifetime, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _appLifetime = appLifetime;
        _env = env;
    }

    public async Task<bool> InstallAsync(SetupViewModel model)
    {
        if (_configuration.GetValue<bool>("IsInstalled"))
            return false;

        string connectionString = string.Empty;

        if (model.DatabaseProvider == "SQLite")
        {
            connectionString = $"Data Source={model.SqliteFilePath}";
        }
        else
        {
            var portPart = model.DbPort.HasValue ? $"Port={model.DbPort.Value};" : "";
            if (model.DatabaseProvider == "MySQL")
                connectionString = $"Server={model.DbHost};{portPart}Database={model.DbName};User={model.DbUsername};Password={model.DbPassword};";
            else // PostgreSQL
                connectionString = $"Host={model.DbHost};{portPart}Database={model.DbName};Username={model.DbUsername};Password={model.DbPassword};";
        }

        // 1. Run Migrations using a temporary DbContext
        switch (model.DatabaseProvider)
        {
            case "PostgreSQL":
                var pgOptions = new DbContextOptionsBuilder<PostgresAppDbContext>();
                pgOptions.UseNpgsql(connectionString, x => x.MigrationsAssembly("Aristokeides.Api"));
                using (var pgCtx = new PostgresAppDbContext(pgOptions.Options))
                {
                    await pgCtx.Database.MigrateAsync();
                    await CreateAdminUser(pgCtx, model);
                }
                break;
            case "MySQL":
                var myOptions = new DbContextOptionsBuilder<MysqlAppDbContext>();
                myOptions.UseMySQL(connectionString, x => x.MigrationsAssembly("Aristokeides.Api"));
                using (var myCtx = new MysqlAppDbContext(myOptions.Options))
                {
                    await myCtx.Database.MigrateAsync();
                    await CreateAdminUser(myCtx, model);
                }
                break;
            case "SQLite":
            default:
                var sqOptions = new DbContextOptionsBuilder<SqliteAppDbContext>();
                sqOptions.UseSqlite(connectionString, x => x.MigrationsAssembly("Aristokeides.Api"));
                using (var sqCtx = new SqliteAppDbContext(sqOptions.Options))
                {
                    await sqCtx.Database.MigrateAsync();
                    await CreateAdminUser(sqCtx, model);
                }
                break;
        }

        // 2. Update appsettings.json
        var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
        var json = await File.ReadAllTextAsync(appSettingsPath);
        var jsonObj = JsonNode.Parse(json)!.AsObject();

        jsonObj["IsInstalled"] = true;
        
        if (jsonObj["Database"] == null)
            jsonObj["Database"] = new JsonObject();
            
        jsonObj["Database"]!["Provider"] = model.DatabaseProvider;
        jsonObj["Database"]!["ConnectionString"] = connectionString;

        await File.WriteAllTextAsync(appSettingsPath, jsonObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        // 3. Request app restart
        _appLifetime.StopApplication();

        return true;
    }

    private async Task CreateAdminUser(AppDbContext context, SetupViewModel model)
    {
        if (await context.Users.AnyAsync(u => u.Role == "Admin"))
            return;

        var adminUser = new User
        {
            Username = model.AdminUsername,
            Email = model.AdminEmail,
            PasswordHash = BC.HashPassword(model.AdminPassword),
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
    }
}
