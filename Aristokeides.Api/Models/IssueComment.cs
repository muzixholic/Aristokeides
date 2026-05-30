namespace Aristokeides.Api.Models;

/// <summary>
/// 이슈 또는 PR에 달리는 코멘트(댓글).
/// </summary>
public class IssueComment
{
    public Guid Id { get; set; }
    
    public Guid IssueId { get; set; }
    public Issue? Issue { get; set; }
    
    public int AuthorId { get; set; }
    public User? Author { get; set; }
    
    public required string Content { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
