using System.Collections.Generic;

namespace Aristokeides.Api.Models;

/// <summary>
/// 조직 내의 팀(Team) 엔터티.
/// </summary>
public class Team
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    
    /// <summary>
    /// 팀 이름.
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// 팀 설명.
    /// </summary>
    public string? Description { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<RepositoryPermission> Permissions { get; set; } = new List<RepositoryPermission>();
}
