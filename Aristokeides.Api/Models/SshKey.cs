using System.Text.Json.Serialization;

namespace Aristokeides.Api.Models;

/// <summary>
/// 사용자의 SSH 공개키 정보를 보관하는 엔티티.
/// </summary>
public class SshKey
{
    public int Id { get; set; }

    /// <summary>
    /// 키를 소유한 사용자의 ID.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// 키를 소유한 사용자 객체.
    /// </summary>
    [JsonIgnore]
    public User? User { get; set; }

    /// <summary>
    /// SSH 키의 이름/라벨.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// SSH 공개키 원본 문자열.
    /// </summary>
    public required string PublicKey { get; set; }

    /// <summary>
    /// SSH 공개키의 SHA-256 지문 (전역 고유 인덱스 설정 대상).
    /// </summary>
    public required string Fingerprint { get; set; }

    /// <summary>
    /// 등록 일시 (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
