using System;
using System.Collections.Generic;

namespace Aristokeides.Api.Models;

/// <summary>
/// 조직(Organization) 엔터티. 여러 저장소와 팀을 가질 수 있는 그룹 단위.
/// </summary>
public class Organization
{
    public int Id { get; set; }
    
    /// <summary>
    /// 조직 이름 (고유값, URL 경로로 사용됨).
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// 조직에 대한 설명.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// 생성 시간.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<Repository> Repositories { get; set; } = new List<Repository>();
}
