using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("MatchSessions")]
    public class MatchSession
    {
        public long MatchSessionId { get; set; }

        public long UserId { get; set; }

        public string Status { get; set; } = null!;

        public decimal MinPrice { get; set; }

        public decimal MaxPrice { get; set; }

        public string? City { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        public User? User { get; set; }

        public ICollection<MatchSessionProduct> OfferingProducts { get; set; } = new HashSet<MatchSessionProduct>();

        public ICollection<MatchSwipe> Swipes { get; set; } = new HashSet<MatchSwipe>();
    }
}
