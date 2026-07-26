using System;

namespace REVORA_BE.DTOs
{
    public class AnnouncementResponseDto
    {
        public long AnnouncementId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string RedirectUrl { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty;
        public string? BadgeText { get; set; }
        public int Priority { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
