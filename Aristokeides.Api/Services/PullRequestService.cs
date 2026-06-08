using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using LibGit2Sharp;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

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

    public virtual async Task<string> GetPullRequestDiffAsync(Guid repositoryId, PullRequest pullRequest)
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
            sb.AppendLine(entry.Patch);
        }

        return sb.ToString();
    }

    public async Task MergePullRequestAsync(Guid repositoryId, PullRequest pullRequest, int userId, bool forceMerge = false)
    {
        // 미해결 토론(IsResolved == false && IsPending == false)이 존재하는지 확인
        var hasUnresolvedComments = await _db.PullRequestReviewComments
            .AnyAsync(c => c.PullRequestId == pullRequest.IssueId && !c.IsResolved && !c.IsPending);

        if (hasUnresolvedComments)
        {
            if (!forceMerge)
            {
                throw new InvalidOperationException("Cannot merge pull request: there are unresolved review discussions.");
            }
            
            // forceMerge가 true인 경우, 요청자 userId의 권한 확인 (Admin만 허용)
            var user = await _db.Users.FindAsync(userId);
            if (user == null || user.Role != "Admin")
            {
                throw new InvalidOperationException("Cannot force-merge pull request: administrator privileges are required.");
            }
        }

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

    public async Task<List<PullRequestReviewComment>> GetReviewCommentsAsync(Guid pullRequestId)
    {
        return await _db.PullRequestReviewComments
            .Include(c => c.Author)
            .Include(c => c.Replies)
            .Where(c => c.PullRequestId == pullRequestId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<PullRequestReviewComment> AddReviewCommentAsync(
        Guid pullRequestId,
        int authorId,
        string content,
        string filePath,
        string? lineType,
        int? oldLineNumber,
        int? newLineNumber,
        string? diffHunk,
        bool isPending = false)
    {
        var pr = await _db.PullRequests
            .Include(p => p.Issue)
            .FirstOrDefaultAsync(p => p.IssueId == pullRequestId);
        if (pr == null)
        {
            throw new ArgumentException("Pull request not found.", nameof(pullRequestId));
        }

        var diffText = await GetPullRequestDiffAsync(pr.Issue!.RepositoryId, pr);
        var diffFiles = DiffParser.Parse(diffText);
        
        bool fileExists = false;
        bool lineExists = false;

        foreach (var file in diffFiles)
        {
            if (string.Equals(file.Path, filePath, StringComparison.OrdinalIgnoreCase))
            {
                fileExists = true;
                foreach (var hunk in file.Hunks)
                {
                    foreach (var line in hunk.Lines)
                    {
                        if (line.OldLineNumber == oldLineNumber && line.NewLineNumber == newLineNumber)
                        {
                            lineExists = true;
                            if (string.IsNullOrEmpty(diffHunk))
                            {
                                diffHunk = hunk.Header;
                            }
                            break;
                        }
                    }
                    if (lineExists) break;
                }
                break;
            }
        }

        if (!fileExists)
        {
            throw new ArgumentException($"File '{filePath}' is not part of the pull request diff.");
        }
        if (!lineExists)
        {
            throw new ArgumentException($"Specified line (Old: {oldLineNumber}, New: {newLineNumber}) does not exist in the diff of file '{filePath}'.");
        }

        var comment = new PullRequestReviewComment
        {
            Id = Guid.NewGuid(),
            PullRequestId = pullRequestId,
            AuthorId = authorId,
            Content = content,
            FilePath = filePath,
            LineType = lineType,
            OldLineNumber = oldLineNumber,
            NewLineNumber = newLineNumber,
            DiffHunk = diffHunk,
            IsResolved = false,
            IsPending = isPending,
            CreatedAt = DateTime.UtcNow
        };

        _db.PullRequestReviewComments.Add(comment);
        await _db.SaveChangesAsync();

        return await _db.PullRequestReviewComments
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id);
    }

    public async Task<PullRequestReviewComment> AddReplyCommentAsync(
        Guid pullRequestId,
        Guid parentId,
        int authorId,
        string content,
        bool isPending = false)
    {
        var parent = await _db.PullRequestReviewComments.FindAsync(parentId);
        if (parent == null)
        {
            throw new ArgumentException("Parent comment not found.", nameof(parentId));
        }

        var comment = new PullRequestReviewComment
        {
            Id = Guid.NewGuid(),
            PullRequestId = pullRequestId,
            AuthorId = authorId,
            Content = content,
            ParentId = parentId,
            FilePath = parent.FilePath,
            LineType = parent.LineType,
            OldLineNumber = parent.OldLineNumber,
            NewLineNumber = parent.NewLineNumber,
            DiffHunk = parent.DiffHunk,
            IsResolved = false,
            IsPending = isPending,
            CreatedAt = DateTime.UtcNow
        };

        _db.PullRequestReviewComments.Add(comment);
        await _db.SaveChangesAsync();

        return await _db.PullRequestReviewComments
            .Include(c => c.Author)
            .FirstAsync(c => c.Id == comment.Id);
    }

    public async Task ResolveReviewCommentAsync(Guid commentId)
    {
        var comment = await _db.PullRequestReviewComments.FindAsync(commentId);
        if (comment == null)
        {
            throw new ArgumentException("Comment not found.", nameof(commentId));
        }

        comment.IsResolved = true;
        await _db.SaveChangesAsync();
    }

    public async Task UnresolveReviewCommentAsync(Guid commentId)
    {
        var comment = await _db.PullRequestReviewComments.FindAsync(commentId);
        if (comment == null)
        {
            throw new ArgumentException("Comment not found.", nameof(commentId));
        }

        comment.IsResolved = false;
        await _db.SaveChangesAsync();
    }

    public async Task<PullRequestReview> SubmitReviewAsync(
        Guid pullRequestId,
        int authorId,
        PullRequestReviewState state,
        string? body)
    {
        // 1. 임시 보관 댓글 일괄 활성화 (IsPending = false)
        var pendingComments = await _db.PullRequestReviewComments
            .Where(c => c.PullRequestId == pullRequestId && c.AuthorId == authorId && c.IsPending)
            .ToListAsync();

        foreach (var comment in pendingComments)
        {
            comment.IsPending = false;
        }

        // 2. 리뷰 데이터 엔터티 생성
        var review = new PullRequestReview
        {
            Id = Guid.NewGuid(),
            PullRequestId = pullRequestId,
            AuthorId = authorId,
            State = state,
            Body = body,
            CreatedAt = DateTime.UtcNow
        };

        _db.PullRequestReviews.Add(review);
        await _db.SaveChangesAsync();

        return review;
    }

    private static readonly Regex HunkHeaderRegex = new(@"^@@\s+-(\d+)(?:,\d+)?\s+\+(\d+)(?:,\d+)?\s+@@");

    public async Task OnBranchPushedAsync(Guid repositoryId, string branchName, string oldOid, string newOid)
    {
        // 소스 브랜치가 branchName이고 병합되지 않은 모든 PR 목록 조회
        var pullRequests = await _db.PullRequests
            .Include(pr => pr.Issue)
            .Where(pr => pr.Issue!.RepositoryId == repositoryId && pr.SourceBranch == branchName && !pr.IsMerged)
            .ToListAsync();

        if (!pullRequests.Any()) return;

        var repoPath = await GetRepoPath(repositoryId);
        if (repoPath == null) return;

        using var repo = new LibGit2Sharp.Repository(repoPath);
        var newCommit = repo.Lookup<Commit>(newOid);
        if (newCommit == null) return; // T-09-02: Lookup 실패 방어

        var oldCommit = !string.IsNullOrEmpty(oldOid) && oldOid != "0000000000000000000000000000000000000000"
            ? repo.Lookup<Commit>(oldOid)
            : null;

        // Diff 비교
        Patch diffPatch;
        if (oldCommit != null)
        {
            diffPatch = repo.Diff.Compare<Patch>(oldCommit.Tree, newCommit.Tree);
        }
        else
        {
            diffPatch = repo.Diff.Compare<Patch>(null, newCommit.Tree);
        }

        var diffText = new StringBuilder();
        foreach (var entry in diffPatch)
        {
            diffText.AppendLine(entry.Patch);
        }

        var diffFiles = DiffParser.Parse(diffText.ToString());

        foreach (var pr in pullRequests)
        {
            // 이 PR에 달린 해결되지 않은 댓글들을 가져옴
            var comments = await _db.PullRequestReviewComments
                .Where(c => c.PullRequestId == pr.IssueId && !c.IsResolved)
                .ToListAsync();

            foreach (var comment in comments)
            {
                var diffFile = diffFiles.FirstOrDefault(f => string.Equals(f.Path, comment.FilePath, StringComparison.OrdinalIgnoreCase));
                if (diffFile != null)
                {
                    int? currentLine = comment.NewLineNumber;
                    bool processed = false;

                    foreach (var hunk in diffFile.Hunks)
                    {
                        if (processed || !currentLine.HasValue) break;

                        var match = HunkHeaderRegex.Match(hunk.Header);
                        if (!match.Success) continue;

                        int oldStart = int.Parse(match.Groups[1].Value);
                        int newStart = int.Parse(match.Groups[2].Value);

                        int oldLen = 0;
                        int newLen = 0;
                        foreach (var l in hunk.Lines)
                        {
                            if (l.OldLineNumber.HasValue) oldLen++;
                            if (l.NewLineNumber.HasValue) newLen++;
                        }

                        if (currentLine < oldStart)
                        {
                            // Hunk보다 앞 라인: 아무 변경 없음
                        }
                        else if (currentLine >= oldStart && currentLine < oldStart + oldLen)
                        {
                            // Hunk 내부에 걸림: 삭제/수정 여부 판단
                            var matchingLine = hunk.Lines.FirstOrDefault(l => l.OldLineNumber == currentLine);
                            if (matchingLine != null && matchingLine.LineType == "Context")
                            {
                                currentLine = matchingLine.NewLineNumber;
                                processed = true;
                            }
                            else
                            {
                                comment.IsOutdated = true;
                                currentLine = null;
                                processed = true;
                            }
                        }
                        else if (currentLine >= oldStart + oldLen)
                        {
                            // Hunk 뒤쪽 라인: 변경량 만큼 Shift
                            currentLine += (newLen - oldLen);
                        }
                    }

                    if (comment.IsOutdated)
                    {
                        // 이미 업데이트됨
                    }
                    else if (currentLine.HasValue && currentLine != comment.NewLineNumber)
                    {
                        comment.NewLineNumber = currentLine;
                    }
                }
            }

            // 새로운 커밋이 소스 브랜치에 푸시되는 경우 기존의 모든 Approved 상태 리뷰는 Dismissed 상태로 초기화된다.
            var approvedReviews = await _db.PullRequestReviews
                .Where(r => r.PullRequestId == pr.IssueId && r.State == PullRequestReviewState.Approved)
                .ToListAsync();

            foreach (var review in approvedReviews)
            {
                review.State = PullRequestReviewState.Dismissed;
            }
        }

        await _db.SaveChangesAsync();
    }
}

