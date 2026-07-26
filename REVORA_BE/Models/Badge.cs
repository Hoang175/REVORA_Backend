using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Badge")]
    public class Badge
    {
        public int BadgeId { get; set; }

        public string Name { get; set; } = null!; // VIP, Newbie, Top-Seller, etc.

        public string IconUrl { get; set; } = null!;

        public string? Description { get; set; }

        public ICollection<UserBadge> UserBadges { get; set; } = new HashSet<UserBadge>();
    }
}
