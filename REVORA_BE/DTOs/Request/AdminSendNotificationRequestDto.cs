using System;
using System.ComponentModel.DataAnnotations;

namespace REVORA_BE.DTOs.Request
{
    public class AdminSendNotificationRequestDto
    {
        [Required]
        public string Type { get; set; } // 'promotion', 'announcement', 'warning', 'event', 'feature'

        [Required]
        public string Target { get; set; } // 'all', 'active', 'new', 'posting_users', 'featured_users'

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime? ScheduledAt { get; set; }

        public List<long>? SpecificUserIds { get; set; }
    }
}
