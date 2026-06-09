using System;

namespace Aristokeides.Api.Models;

/// <summary>
/// 저장소별 개인 또는 팀의 권한을 관리하는 엔터티.
/// </summary>
public class RepositoryPermission
{
    public int Id { get; set; }
    public Guid RepositoryId { get; set; }
    public int? UserId { get; set; }
    public int? TeamId { get; set; }
    
    /// <summary>
    /// 권한 수준: Read, Write, Admin
    /// </summary>
    public required string AccessLevel { get; set; } // "Read", "Write", "Admin"

    public Repository Repository { get; set; } = null!;
    public User? User { get; set; }
    public Team? Team { get; set; }
}
