using System;

namespace Aristokeides.Api.Models;

public class UserSession
{
    public required string Id { get; set; } // 암호학적 토큰 또는 Guid
    public int UserId { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActiveAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;

    public User User { get; set; } = null!;
}
