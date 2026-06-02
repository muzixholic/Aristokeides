namespace Aristokeides.Api.Models;

/// <summary>
/// 시스템 사용자 엔터티. 인증 및 권한 관리의 기본 단위.
/// </summary>
public class User
{
    public int Id { get; set; }

    /// <summary>
    /// 사용자 이메일 (로그인 식별자, 고유값).
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// BCrypt 해시된 비밀번호.
    /// </summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// 사용자 역할: Admin, Contributor, Reader.
    /// </summary>
    public required string Role { get; set; } = "Reader";

    /// <summary>
    /// 계정 생성 시간 (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public required string Username { get; set; }

    public ICollection<Repository> Repositories { get; set; } = new List<Repository>();
    public ICollection<Issue> CreatedIssues { get; set; } = new List<Issue>();
    public ICollection<Issue> AssignedIssues { get; set; } = new List<Issue>();
    public ICollection<SshKey> SshKeys { get; set; } = new List<SshKey>();
}
