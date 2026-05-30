namespace Aristokeides.Api.Models;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// 풀 리퀘스트 엔터티. Issue와 1:1 관계를 가집니다.
/// </summary>
public class PullRequest
{
    [Key]
    public Guid IssueId { get; set; }
    
    [Required]
    [MaxLength(256)]
    public string SourceBranch { get; set; } = null!;
    
    [Required]
    [MaxLength(256)]
    public string TargetBranch { get; set; } = null!;
    
    public bool IsMerged { get; set; }
    
    [MaxLength(40)]
    public string? MergeCommitSha { get; set; }
    
    [ForeignKey("IssueId")]
    public Issue? Issue { get; set; }
}
