using System.ComponentModel.DataAnnotations;

namespace Aristokeides.Api.Models;

public class CommitSignature
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public required string CommitHash { get; set; }

    public Guid RepositoryId { get; set; }
    public Repository? Repository { get; set; }

    public int? SignerUserId { get; set; }
    public User? SignerUser { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Status { get; set; } = "NoSignature";

    [MaxLength(50)]
    public string? Algorithm { get; set; }

    [MaxLength(256)]
    public string? KeyFingerprint { get; set; }

    public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
}
