using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("RefreshTokens")]
    public class RefreshToken
    {
        public long Id { get; set; }

        public string Token { get; set; } = null!;

        public long UserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsRevoked { get; set; }

        public DateTime? RevokedAt { get; set; }

        public string? DeviceName { get; set; }

        public string? IpAddress { get; set; }

        public User User { get; set; } = null!;
    }
}
