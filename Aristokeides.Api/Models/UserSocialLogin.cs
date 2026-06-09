namespace Aristokeides.Api.Models;

public class UserSocialLogin
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Provider { get; set; } // "GitHub", "Google"
    public required string ProviderKey { get; set; } // 외부 사용자 고유 ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
