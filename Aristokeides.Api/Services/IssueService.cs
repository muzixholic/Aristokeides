using Aristokeides.Api.Data;
using Aristokeides.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Aristokeides.Api.Services;

/// <summary>
/// 이슈 관련 비즈니스 로직 및 DB 액세스 서비스.
/// </summary>
public class IssueService
{
    private readonly AppDbContext _db;

    public IssueService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// 저장소의 모든 이슈를 가져옵니다 (칸반 보드 표시용).
    /// </summary>
    public async Task<List<Issue>> GetIssuesAsync(Guid repositoryId)
    {
        return await _db.Issues
            .Where(i => i.RepositoryId == repositoryId)
            .Include(i => i.Assignee)
            .Include(i => i.Column)
            .OrderBy(i => i.LocalId)
            .ToListAsync();
    }

    /// <summary>
    /// 저장소의 칸반 보드 열 목록을 가져옵니다.
    /// </summary>
    public async Task<List<BoardColumn>> GetBoardColumnsAsync(Guid repositoryId)
    {
        return await _db.BoardColumns
            .Where(bc => bc.RepositoryId == repositoryId)
            .OrderBy(bc => bc.Order)
            .ToListAsync();
    }

    /// <summary>
    /// 특정 이슈를 LocalId로 조회합니다.
    /// </summary>
    public async Task<Issue?> GetIssueAsync(Guid repositoryId, int localId)
    {
        return await _db.Issues
            .Where(i => i.RepositoryId == repositoryId && i.LocalId == localId)
            .Include(i => i.Creator)
            .Include(i => i.Assignee)
            .Include(i => i.Column)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 새 이슈를 생성합니다. LocalId는 저장소 내 최대값 + 1로 자동 할당됩니다.
    /// </summary>
    public async Task<Issue> CreateIssueAsync(Guid repositoryId, string title, string? description, int creatorId)
    {
        // Fetch max LocalId for this repository
        var maxLocalId = await _db.Issues
            .Where(i => i.RepositoryId == repositoryId)
            .MaxAsync(i => (int?)i.LocalId) ?? 0;

        // Default to "To Do" column
        var toDoColumn = await _db.BoardColumns
            .Where(bc => bc.RepositoryId == repositoryId && bc.Name == "To Do")
            .FirstAsync();

        var issue = new Issue
        {
            Id = Guid.NewGuid(),
            LocalId = maxLocalId + 1,
            RepositoryId = repositoryId,
            Title = title,
            Description = description,
            CreatorId = creatorId,
            ColumnId = toDoColumn.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Issues.Add(issue);
        await _db.SaveChangesAsync();

        return issue;
    }

    /// <summary>
    /// 이슈의 칸반 보드 열(상태)을 변경합니다 (드래그 앤 드롭 지원).
    /// </summary>
    public async Task<bool> UpdateIssueStatusAsync(Guid issueId, Guid newColumnId)
    {
        var issue = await _db.Issues.FindAsync(issueId);
        if (issue == null) return false;

        issue.ColumnId = newColumnId;
        issue.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 이슈의 제목, 설명, 담당자를 수정합니다.
    /// </summary>
    public async Task<bool> UpdateIssueDetailsAsync(Guid issueId, string title, string? description, int? assigneeId)
    {
        var issue = await _db.Issues.FindAsync(issueId);
        if (issue == null) return false;

        issue.Title = title;
        issue.Description = description;
        issue.AssigneeId = assigneeId;
        issue.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 이슈를 닫습니다 ("Done" 열로 이동).
    /// </summary>
    public async Task<bool> CloseIssueAsync(Guid issueId)
    {
        var issue = await _db.Issues
            .Include(i => i.Column)
            .FirstOrDefaultAsync(i => i.Id == issueId);

        if (issue == null) return false;

        var doneColumn = await _db.BoardColumns
            .Where(bc => bc.RepositoryId == issue.RepositoryId && bc.Name == "Done")
            .FirstOrDefaultAsync();

        if (doneColumn == null) return false;

        issue.ColumnId = doneColumn.Id;
        issue.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 저장소 정보를 사용자명과 저장소명으로 조회합니다.
    /// </summary>
    public async Task<Repository?> GetRepositoryAsync(string username, string repoName)
    {
        return await _db.Repositories
            .Include(r => r.Owner)
            .FirstOrDefaultAsync(r => r.Name == repoName && r.Owner!.Username == username);
    }

    /// <summary>
    /// 저장소에 접근 가능한 사용자 목록을 조회합니다 (담당자 선택용).
    /// </summary>
    public async Task<List<User>> GetAssignableUsersAsync()
    {
        return await _db.Users.OrderBy(u => u.Username).ToListAsync();
    }
}
