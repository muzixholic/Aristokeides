using System;

namespace Aristokeides.Api.Models;

public class LfsLock
{
    public int Id { get; set; }
    public Guid RepositoryId { get; set; }
    public int UserId { get; set; }
    public required string Path { get; set; }
    public DateTime LockedAt { get; set; } = DateTime.UtcNow;

    public Repository Repository { get; set; } = null!;
    public User User { get; set; } = null!;
}
