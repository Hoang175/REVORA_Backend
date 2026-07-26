using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    public class Announcement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long AnnouncementId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Required]
        [StringLength(300)]
        public string RedirectUrl { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ButtonText { get; set; } = string.Empty;

        [StringLength(50)]
        public string? BadgeText { get; set; }


        public int Priority { get; set; } = 0;

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
