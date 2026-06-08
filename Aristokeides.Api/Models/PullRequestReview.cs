using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aristokeides.Api.Models;

public enum PullRequestReviewState
{
    Comment,
    Approved,
    ChangesRequested,
    Dismissed
}

public class PullRequestReview
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
    public PullRequestReviewState State { get; set; }

    public string? Body { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
