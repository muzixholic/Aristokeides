using System.Text;
using Aristokeides.Api.Data;
using Aristokeides.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

// --- Database ---
var dbProvider = builder.Configuration["Database:Provider"] ?? "SQLite";
var dbConnectionString = builder.Configuration["Database:ConnectionString"] ?? "Data Source=aristokeides.db";

switch (dbProvider.ToLowerInvariant())
{
    case "postgresql":
    case "postgres":
        builder.Services.AddDbContext<AppDbContext, PostgresAppDbContext>(options =>
            options.UseNpgsql(dbConnectionString, x => x.MigrationsAssembly("Aristokeides.Api")));
        break;
    case "mysql":
    case "mariadb":
        builder.Services.AddDbContext<AppDbContext, MysqlAppDbContext>(options =>
            options.UseMySQL(dbConnectionString, x => x.MigrationsAssembly("Aristokeides.Api")));
        break;
    case "sqlite":
    default:
        builder.Services.AddDbContext<AppDbContext, SqliteAppDbContext>(options =>
            options.UseSqlite(dbConnectionString, x => x.MigrationsAssembly("Aristokeides.Api")));
        break;
}

// --- Authentication (JWT and Basic) ---
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "JWT_OR_COOKIE";
    options.DefaultChallengeScheme = "JWT_OR_COOKIE";
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
})
.AddCookie("Cookies", opts => 
{
    opts.LoginPath = "/login";
})
.AddPolicyScheme("JWT_OR_COOKIE", "JWT_OR_COOKIE", opts =>
{
    opts.ForwardDefaultSelector = context =>
    {
        string authorization = context.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
            return JwtBearerDefaults.AuthenticationScheme;
        
        return "Cookies";
    };
})
.AddScheme<AuthenticationSchemeOptions, Aristokeides.Api.Auth.BasicAuthenticationHandler>("Basic", null);

builder.Services.AddCascadingAuthenticationState();


builder.Services.AddAuthorization();

// --- Background Services ---
builder.Services.AddSingleton<RepositoryCreationChannel>();
builder.Services.AddHostedService<RepositoryCreationBackgroundWorker>();
builder.Services.AddSingleton<Aristokeides.Api.Services.Ssh.SshUrlHelper>();
builder.Services.AddTransient<Aristokeides.Api.Services.Ssh.SshCommandBridge>();
builder.Services.AddHostedService<Aristokeides.Api.Services.Ssh.SshServerBackgroundService>();
builder.Services.AddSingleton<Aristokeides.Api.Services.Ssh.SshSignatureVerificationService>();
builder.Services.AddScoped<GitBrowserService>();
builder.Services.AddScoped<IssueService>();
builder.Services.AddScoped<PullRequestService>();
builder.Services.AddScoped<SetupService>();
builder.Services.AddScoped<AdminSettingsService>();

// --- Controllers ---
builder.Services.AddControllers();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntiforgery();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// --- Swagger with JWT support ---
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aristokeides API",
        Version = "v1",
        Description = "Git 기반 프로젝트 관리 시스템 API"
    });

    // JWT Bearer 토큰 인증 지원
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT 토큰을 입력하세요. 예: eyJhbGciOiJIUzI1NiIs..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

var isInstalled = app.Configuration.GetValue<bool>("IsInstalled");

// --- Auto-Migration on startup ---
if (isInstalled)
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}

// --- HTTP Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<Aristokeides.Api.Middleware.SetupRedirectMiddleware>();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Middleware for Git Smart HTTP
app.UseMiddleware<Aristokeides.Api.Middleware.GitSmartHttpMiddleware>();

app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<Aristokeides.Api.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
