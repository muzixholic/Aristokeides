namespace Aristokeides.Api.Models;

/// <summary>
/// 팀원(TeamMember) 매핑 엔터티.
/// </summary>
public class TeamMember
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public int UserId { get; set; }

    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}
