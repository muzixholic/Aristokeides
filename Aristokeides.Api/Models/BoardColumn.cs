namespace Aristokeides.Api.Models;

/// <summary>
/// 칸반 보드 열. 각 저장소에 기본 열(To Do, In Progress, Done)이 생성됨.
/// </summary>
public class BoardColumn
{
    public Guid Id { get; set; }
    public Guid RepositoryId { get; set; }
    public required string Name { get; set; }
    public int Order { get; set; }

    public Repository? Repository { get; set; }
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
