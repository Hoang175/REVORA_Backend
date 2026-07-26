using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("TradeMatches")]
    public class TradeMatch
    {
        public long TradeMatchId { get; set; }

        public long UserLowId { get; set; }

        public long UserHighId { get; set; }

        public long? ProductLowUserId { get; set; }

        public long? ProductHighUserId { get; set; }

        public long SessionLowUserId { get; set; }

        public long SessionHighUserId { get; set; }

        public long? ConversationId { get; set; }

        public string Status { get; set; } = null!;

        public bool LowUserConfirmed { get; set; }

        public bool HighUserConfirmed { get; set; }

        public bool LowUserNegotiateConfirmed { get; set; }

        public bool HighUserNegotiateConfirmed { get; set; }

        public string? LowUserSelectedProductIds { get; set; }

        public string? HighUserSelectedProductIds { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public User? UserLow { get; set; }

        public User? UserHigh { get; set; }

        public Product? ProductLowUser { get; set; }

        public Product? ProductHighUser { get; set; }

        public Conversation? Conversation { get; set; }
    }
}
