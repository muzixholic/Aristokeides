namespace Aristokeides.Api.Models;

public class Repository
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int OwnerId { get; set; } // OwnerId should probably match User.Id, which is int
    public required string Status { get; set; } = "Creating";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Owner { get; set; }
}
