using System;

namespace Aristokeides.Api.Models;

public class LfsObject
{
    public int Id { get; set; }
    public Guid RepositoryId { get; set; }
    public required string Oid { get; set; } // SHA-256 (64자)
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Repository Repository { get; set; } = null!;
}
