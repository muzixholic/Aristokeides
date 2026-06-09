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

    /// <summary>
    /// 2FA 활성화 여부
    /// </summary>
    public bool IsTwoFactorEnabled { get; set; } = false;

    /// <summary>
    /// 2FA TOTP용 Base32 비밀키
    /// </summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// 쉼표로 구분되는 백업 복구 코드 목록
    /// </summary>
    public string? TwoFactorRecoveryCodes { get; set; }

    public ICollection<Repository> Repositories { get; set; } = new List<Repository>();
    public ICollection<Issue> CreatedIssues { get; set; } = new List<Issue>();
    public ICollection<Issue> AssignedIssues { get; set; } = new List<Issue>();
    public ICollection<SshKey> SshKeys { get; set; } = new List<SshKey>();
    public ICollection<UserSocialLogin> SocialLogins { get; set; } = new List<UserSocialLogin>();
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();
}

