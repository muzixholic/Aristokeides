namespace Aristokeides.Api.Models;

public class Repository
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int OwnerId { get; set; } // OwnerId should probably match User.Id, which is int
    public required string Status { get; set; } = "Creating";
    public bool RequireSignedCommits { get; set; } = false;
    public bool IsPrivate { get; set; } = true;
    public string? PrimaryLanguage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User? Owner { get; set; }
    public ICollection<BoardColumn> BoardColumns { get; set; } = new List<BoardColumn>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
