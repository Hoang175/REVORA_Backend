using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("UserBadge")]
    public class UserBadge
    {
        public int UserBadgeId { get; set; }

        public int BadgeId { get; set; }

        public long UserId { get; set; }

        public DateTime? ExpiredAt { get; set; }

        public Badge? Badge { get; set; }

        public User? User { get; set; }
    }
}
