using System;
using System.ComponentModel.DataAnnotations;

namespace Aristokeides.Api.Models
{
    public class SshAuthLog
    {
        public int Id { get; set; }
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
        [MaxLength(45)]
        public string ClientIp { get; set; } = string.Empty;
        public string? KeyFingerprint { get; set; }
        public string? Username { get; set; }
        public bool IsSuccess { get; set; }
        public string? FailureReason { get; set; }
        public string? KeyType { get; set; }  // ssh-rsa, ssh-ed25519, ecdsa-sha2-nistp256
    }
}
