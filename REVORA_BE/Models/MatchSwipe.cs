using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("MatchSwipes")]
    public class MatchSwipe
    {
        public long MatchSwipeId { get; set; }

        public long MatchSessionId { get; set; }

        public long UserId { get; set; }

        public long TargetProductId { get; set; }

        public string Direction { get; set; } = null!;

        public DateTime SwipedAt { get; set; }

        public MatchSession? MatchSession { get; set; }

        public User? User { get; set; }

        public Product? TargetProduct { get; set; }
    }
}
