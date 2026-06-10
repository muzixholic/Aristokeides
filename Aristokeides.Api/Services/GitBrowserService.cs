using System.Text;
using Aristokeides.Api.Data;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;

namespace Aristokeides.Api.Services;

public record GitTreeEntry(string Name, string Path, bool IsFolder, string ObjectId);

public class GitBlobInfo
{
    public string Path { get; set; } = "";
    public string? TextContent { get; set; }
    public bool IsBinary { get; set; }
    public bool IsLfsPointer { get; set; }
    public string? LfsOid { get; set; }
    public long LfsSize { get; set; }
}

public record GitCommitInfo(
    string Hash, 
    string Message, 
    string Author, 
    DateTimeOffset Date,
    string? SignatureStatus = null,
    string? SignatureFingerprint = null,
    string? SignerUsername = null
);

public class GitBrowserService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public GitBrowserService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<string?> ValidateAccessAsync(int userId, string username, string repoName)
    {
        var repo = await _db.Repositories
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.Name == repoName && r.Owner!.Username == username);

        if (repo == null || repo.OwnerId != userId)
            return null;

        var basePath = _config["GitSettings:BasePath"] ?? Path.GetFullPath("GitRepos");
        return Path.Combine(basePath, username, $"{repoName}.git");
    }

    public List<string> GetBranches(string repoPath)
    {
        using var repo = new Repository(repoPath);
        return repo.Branches.Select(b => b.FriendlyName).ToList();
    }

    public string? GetDefaultBranch(string repoPath)
    {
        using var repo = new Repository(repoPath);
        return repo.Head?.FriendlyName;
    }

    public List<GitTreeEntry> GetTreeEntries(string repoPath, string branch, string path)
    {
        using var repo = new Repository(repoPath);
        var branchRef = repo.Branches[branch];
        if (branchRef == null || branchRef.Tip == null) return new List<GitTreeEntry>();

        Tree tree = branchRef.Tip.Tree;

        if (!string.IsNullOrEmpty(path))
        {
            var entry = branchRef.Tip[path];
            if (entry == null || entry.TargetType != TreeEntryTargetType.Tree)
                return new List<GitTreeEntry>();
            tree = (Tree)entry.Target;
        }

        return tree.Select(e => new GitTreeEntry(
            e.Name,
            e.Path,
            e.TargetType == TreeEntryTargetType.Tree,
            e.Target.Sha
        ))
        .OrderByDescending(e => e.IsFolder)
        .ThenBy(e => e.Name)
        .ToList();
    }

    public string? GetBlobContent(string repoPath, string branch, string path)
    {
        using var repo = new Repository(repoPath);
        var branchRef = repo.Branches[branch];
        if (branchRef == null || branchRef.Tip == null) return null;

        var entry = branchRef.Tip[path];
        if (entry == null || entry.TargetType != TreeEntryTargetType.Blob) return null;

        var blob = (Blob)entry.Target;
        if (blob.IsBinary) return null;

        return blob.GetContentText();
    }

    public GitBlobInfo? GetBlobInfo(string repoPath, string branch, string path)
    {
        using var repo = new Repository(repoPath);
        var branchRef = repo.Branches[branch];
        if (branchRef == null || branchRef.Tip == null) return null;

        var entry = branchRef.Tip[path];
        if (entry == null || entry.TargetType != TreeEntryTargetType.Blob) return null;

        var blob = (Blob)entry.Target;
        
        var result = new GitBlobInfo
        {
            Path = path,
            IsBinary = blob.IsBinary
        };

        if (!blob.IsBinary)
        {
            var content = blob.GetContentText();
            result.TextContent = content;

            if (content != null && content.Length <= 300 && content.StartsWith("version https://git-lfs.github.com/spec/v1"))
            {
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                string? oid = null;
                long size = 0;
                foreach (var line in lines)
                {
                    if (line.StartsWith("oid sha256:"))
                    {
                        oid = line.Substring("oid sha256:".Length).Trim();
                    }
                    else if (line.StartsWith("size "))
                    {
                        long.TryParse(line.Substring("size ".Length).Trim(), out size);
                    }
                }

                if (!string.IsNullOrEmpty(oid) && oid.Length == 64)
                {
                    result.IsLfsPointer = true;
                    result.LfsOid = oid;
                    result.LfsSize = size;
                }
            }
        }

        return result;
    }

    public async Task<(List<GitCommitInfo> Commits, bool HasNextPage)> GetCommitsAsync(string repoPath, string branch, int page, int pageSize)
    {
        using var repo = new Repository(repoPath);
        var branchRef = repo.Branches[branch];
        if (branchRef == null || branchRef.Tip == null) return (new List<GitCommitInfo>(), false);

        var filter = new CommitFilter
        {
            IncludeReachableFrom = branchRef.Tip,
            SortBy = CommitSortStrategies.Topological
        };

        var allCommits = repo.Commits.QueryBy(filter);
        var pagedCommits = allCommits.Skip((page - 1) * pageSize).Take(pageSize + 1).ToList();

        var hasNextPage = pagedCommits.Count > pageSize;
        var displayCommits = pagedCommits.Take(pageSize).ToList();

        var hashes = displayCommits.Select(c => c.Sha).ToList();

        var signatures = await _db.CommitSignatures
            .Include(s => s.SignerUser)
            .Where(s => hashes.Contains(s.CommitHash))
            .ToDictionaryAsync(s => s.CommitHash, s => s);

        var commits = displayCommits.Select(c =>
        {
            signatures.TryGetValue(c.Sha, out var sig);
            return new GitCommitInfo(
                c.Sha,
                c.MessageShort,
                c.Author.Name,
                c.Author.When,
                sig?.Status,
                sig?.KeyFingerprint,
                sig?.SignerUser?.Username
            );
        }).ToList();

        return (commits, hasNextPage);
    }
}
