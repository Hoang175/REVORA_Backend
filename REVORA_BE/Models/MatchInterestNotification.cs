using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("MatchInterestNotifications")]
    public class MatchInterestNotification
    {
        public long MatchInterestNotificationId { get; set; }

        public long OwnerUserId { get; set; }

        public long InterestedUserId { get; set; }

        public long LikedProductId { get; set; }

        public long OfferingProductId { get; set; }

        public long MatchSessionId { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; }

        public User? OwnerUser { get; set; }

        public User? InterestedUser { get; set; }

        public Product? LikedProduct { get; set; }

        public Product? OfferingProduct { get; set; }
    }
}
