using System;

namespace Aristokeides.Api.Models;

/// <summary>
/// 조직 구성원(OrganizationMember) 매핑 엔터티.
/// </summary>
public class OrganizationMember
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public int UserId { get; set; }
    
    /// <summary>
    /// 멤버의 역할: Owner, Member 등.
    /// </summary>
    public required string Role { get; set; } = "Member"; // "Owner", "Member"
    
    /// <summary>
    /// 가입일.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}
