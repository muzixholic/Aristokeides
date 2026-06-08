using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aristokeides.Api.Models;

/// <summary>
/// PR Diff 인라인 댓글 엔터티.
/// </summary>
public class PullRequestReviewComment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid PullRequestId { get; set; }

    [ForeignKey("PullRequestId")]
    public PullRequest? PullRequest { get; set; }

    [Required]
    public int AuthorId { get; set; }

    [ForeignKey("AuthorId")]
    public User? Author { get; set; }

    [Required]
    [MaxLength(1024)]
    public string FilePath { get; set; } = null!;

    public int? OldLineNumber { get; set; }

    public int? NewLineNumber { get; set; }

    [MaxLength(50)]
    public string? LineType { get; set; }

    public string? DiffHunk { get; set; }

    [Required]
    public string Content { get; set; } = null!;

    public bool IsResolved { get; set; }

    public Guid? ParentId { get; set; }

    [ForeignKey("ParentId")]
    public PullRequestReviewComment? Parent { get; set; }

    public ICollection<PullRequestReviewComment> Replies { get; set; } = new List<PullRequestReviewComment>();

    public bool IsPending { get; set; }

    public bool IsOutdated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
