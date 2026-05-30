namespace Aristokeides.Api.Models;

/// <summary>
/// 이슈 엔터티. 각 저장소에 대한 이슈 트래킹을 담당.
/// </summary>
public class Issue
{
    public Guid Id { get; set; }

    /// <summary>
    /// 저장소 내 순차적 이슈 번호 (예: #1, #2, #3).
    /// </summary>
    public int LocalId { get; set; }

    public Guid RepositoryId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// 이슈 작성자 ID.
    /// </summary>
    public int CreatorId { get; set; }

    /// <summary>
    /// 이슈 담당자 ID (선택 사항).
    /// </summary>
    public int? AssigneeId { get; set; }

    /// <summary>
    /// 이슈가 속한 칸반 보드 열 ID.
    /// </summary>
    public Guid ColumnId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Repository? Repository { get; set; }
    public User? Creator { get; set; }
    public User? Assignee { get; set; }
    public BoardColumn? Column { get; set; }
    public PullRequest? PullRequest { get; set; }
    public ICollection<IssueComment> Comments { get; set; } = new List<IssueComment>();
}
