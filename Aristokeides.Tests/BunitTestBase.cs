using System;
using System.Threading.Tasks;
using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Aristokeides.Api.Services;
using Bunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aristokeides.Tests;

public class BunitTestBase : BunitContext, IDisposable
{
    public AppDbContext DbContext { get; }

    public BunitTestBase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Aristokeides_Bunit_Test_{Guid.NewGuid()}")
            .Options;

        DbContext = new AppDbContext(options);

        // DI 등록
        Services.AddSingleton<AppDbContext>(DbContext);
        Services.AddSingleton<RepositoryCreationChannel>();
        Services.AddScoped<SetupService, FakeSetupService>();
        Services.AddScoped<IssueService, FakeIssueService>();
    }

    public new void Dispose()
    {
        DbContext.Dispose();
        base.Dispose();
    }
}

public class FakeSetupService : SetupService
{
    public bool InstallCalled { get; private set; }
    public SetupViewModel? InstalledModel { get; private set; }

    public FakeSetupService() : base(null!, null!, null!) { }

    public override Task<bool> InstallAsync(SetupViewModel model)
    {
        InstallCalled = true;
        InstalledModel = model;
        return Task.FromResult(true);
    }
}

public class FakeIssueService : IssueService
{
    public bool CreateCalled { get; private set; }
    public string? CreatedTitle { get; private set; }

    public FakeIssueService() : base(null!, null!) { }

    public override Task<Issue> CreateIssueAsync(Guid repositoryId, string title, string? description, int creatorId, int? assigneeId = null)
    {
        CreateCalled = true;
        CreatedTitle = title;
        return Task.FromResult(new Issue
        {
            Id = Guid.NewGuid(),
            RepositoryId = repositoryId,
            Title = title,
            Description = description,
            CreatorId = creatorId,
            LocalId = 1
        });
    }
}
