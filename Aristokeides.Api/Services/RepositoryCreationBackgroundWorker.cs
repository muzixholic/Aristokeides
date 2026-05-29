using Aristokeides.Api.Data;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;

namespace Aristokeides.Api.Services;

public class RepositoryCreationBackgroundWorker : BackgroundService
{
    private readonly RepositoryCreationChannel _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RepositoryCreationBackgroundWorker> _logger;

    public RepositoryCreationBackgroundWorker(
        RepositoryCreationChannel channel,
        IServiceProvider serviceProvider,
        ILogger<RepositoryCreationBackgroundWorker> logger)
    {
        _channel = channel;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var repoId in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var repo = await db.Repositories
                    .Include(r => r.Owner)
                    .FirstOrDefaultAsync(r => r.Id == repoId, stoppingToken);

                if (repo == null || repo.Owner == null)
                {
                    _logger.LogWarning("Repository or Owner not found for Id {RepoId}", repoId);
                    continue;
                }

                var username = repo.Owner.Username;
                var repoName = repo.Name;
                var gitPath = $"C:/GitRepos/{username}/{repoName}.git";

                Directory.CreateDirectory(Path.GetDirectoryName(gitPath)!);
                LibGit2Sharp.Repository.Init(gitPath, isBare: true);

                repo.Status = "Ready";
                await db.SaveChangesAsync(stoppingToken);

                _logger.LogInformation("Successfully initialized git repo for {RepoId} at {Path}", repoId, gitPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create git repository for Id {RepoId}", repoId);
                
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var repo = await db.Repositories.FindAsync(new object[] { repoId }, stoppingToken);
                    if (repo != null)
                    {
                        repo.Status = "Error";
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Failed to update repository status to Error for Id {RepoId}", repoId);
                }
            }
        }
    }
}
