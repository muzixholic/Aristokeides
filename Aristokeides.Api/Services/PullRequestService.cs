using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Aristokeides.Api.Services;

public class PullRequestService
{
    private readonly AppDbContext _db;
    private readonly IssueService _issueService;
    private readonly GitBrowserService _gitService;
    private readonly IConfiguration _config;

    public PullRequestService(AppDbContext db, IssueService issueService, GitBrowserService gitService, IConfiguration config)
    {
        _db = db;
        _issueService = issueService;
        _gitService = gitService;
        _config = config;
    }

    private async Task<string?> GetRepoPath(Guid repositoryId)
    {
        var repo = await _db.Repositories.Include(r => r.Owner).FirstOrDefaultAsync(r => r.Id == repositoryId);
        if (repo == null) return null;
        var basePath = _config["GitSettings:BasePath"] ?? Path.GetFullPath("GitRepos");
        return Path.Combine(basePath, repo.Owner!.Username, $"{repo.Name}.git");
    }

    public async Task<List<PullRequest>> GetPullRequestsAsync(Guid repositoryId)
    {
        return await _db.PullRequests
            .Include(pr => pr.Issue)
            .ThenInclude(i => i!.Creator)
            .Where(pr => pr.Issue!.RepositoryId == repositoryId)
            .OrderByDescending(pr => pr.Issue!.LocalId)
            .ToListAsync();
    }

    public async Task<PullRequest?> GetPullRequestAsync(Guid repositoryId, int localId)
    {
        return await _db.PullRequests
            .Include(pr => pr.Issue)
            .ThenInclude(i => i!.Creator)
            .Include(pr => pr.Issue!.Comments)
            .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(pr => pr.Issue!.RepositoryId == repositoryId && pr.Issue.LocalId == localId);
    }

    public async Task<PullRequest> CreatePullRequestAsync(Guid repositoryId, string title, string? description, string sourceBranch, string targetBranch, int creatorId)
    {
        var repoPath = await GetRepoPath(repositoryId);
        if (repoPath == null) throw new InvalidOperationException("Repository not found.");

        using var repo = new LibGit2Sharp.Repository(repoPath);
        if (repo.Branches[sourceBranch] == null) throw new InvalidOperationException("Source branch does not exist.");
        if (repo.Branches[targetBranch] == null) throw new InvalidOperationException("Target branch does not exist.");

        var issue = await _issueService.CreateIssueAsync(repositoryId, title, description, creatorId);

        var pr = new PullRequest
        {
            IssueId = issue.Id,
            SourceBranch = sourceBranch,
            TargetBranch = targetBranch,
            IsMerged = false
        };

        _db.PullRequests.Add(pr);
        await _db.SaveChangesAsync();

        return pr;
    }

    public async Task<bool> CheckConflictAsync(Guid repositoryId, PullRequest pullRequest)
    {
        var repoPath = await GetRepoPath(repositoryId);
        if (repoPath == null) return false;

        using var repo = new LibGit2Sharp.Repository(repoPath);
        var sourceCommit = repo.Branches[pullRequest.SourceBranch]?.Tip;
        var targetCommit = repo.Branches[pullRequest.TargetBranch]?.Tip;

        if (sourceCommit == null || targetCommit == null) return false;

        var mergeResult = repo.ObjectDatabase.MergeCommits(targetCommit, sourceCommit, new MergeTreeOptions());
        return mergeResult.Status == MergeTreeStatus.Conflicts;
    }

    public async Task<string> GetPullRequestDiffAsync(Guid repositoryId, PullRequest pullRequest)
    {
        var repoPath = await GetRepoPath(repositoryId);
        if (repoPath == null) return string.Empty;

        using var repo = new LibGit2Sharp.Repository(repoPath);
        var sourceTree = repo.Branches[pullRequest.SourceBranch]?.Tip?.Tree;
        var targetTree = repo.Branches[pullRequest.TargetBranch]?.Tip?.Tree;

        if (sourceTree == null || targetTree == null) return string.Empty;

        var patch = repo.Diff.Compare<Patch>(targetTree, sourceTree);
        var sb = new StringBuilder();

        foreach (var entry in patch)
        {
            sb.AppendLine($"--- a/{entry.Path}");
            sb.AppendLine($"+++ b/{entry.Path}");
            sb.AppendLine(entry.Patch);
        }

        return sb.ToString();
    }

    public async Task MergePullRequestAsync(Guid repositoryId, PullRequest pullRequest, int userId)
    {
        var repoPath = await GetRepoPath(repositoryId);
        if (repoPath == null) throw new InvalidOperationException("Repository not found.");

        using var repo = new LibGit2Sharp.Repository(repoPath);
        var sourceCommit = repo.Branches[pullRequest.SourceBranch]?.Tip;
        var targetCommit = repo.Branches[pullRequest.TargetBranch]?.Tip;

        if (sourceCommit == null || targetCommit == null) throw new InvalidOperationException("Branch missing.");

        var mergeResult = repo.ObjectDatabase.MergeCommits(targetCommit, sourceCommit, new MergeTreeOptions());
        if (mergeResult.Status == MergeTreeStatus.Conflicts)
        {
            throw new InvalidOperationException("Merge conflict detected. Please resolve conflicts locally before merging.");
        }

        var author = await _db.Users.FindAsync(userId);
        var signature = new Signature(author?.Username ?? "Aristokeides", author?.Email ?? "noreply@aristokeides.local", DateTimeOffset.Now);

        var mergeCommit = repo.ObjectDatabase.CreateCommit(
            signature, signature, $"Merge pull request #{pullRequest.Issue?.LocalId} from {pullRequest.SourceBranch}",
            mergeResult.Tree, new[] { targetCommit, sourceCommit }, false);

        repo.Refs.UpdateTarget(repo.Branches[pullRequest.TargetBranch].Reference, mergeCommit.Id);

        pullRequest.IsMerged = true;
        pullRequest.MergeCommitSha = mergeCommit.Sha;
        await _db.SaveChangesAsync();

        await _issueService.CloseIssueAsync(pullRequest.IssueId);
    }
}
