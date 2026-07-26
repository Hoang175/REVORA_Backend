using System;
using System.ComponentModel.DataAnnotations;

namespace REVORA_BE.DTOs.Request
{
    public class AnnouncementCreateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = null!;

        [Required]
        public string ImageUrl { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string RedirectUrl { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ButtonText { get; set; } = null!;

        [MaxLength(50)]
        public string? BadgeText { get; set; }


        public int Priority { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public bool IsActive { get; set; }
    }

    public class AnnouncementUpdateDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = null!;

        [Required]
        public string ImageUrl { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string RedirectUrl { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ButtonText { get; set; } = null!;

        [MaxLength(50)]
        public string? BadgeText { get; set; }

        public int Priority { get; set; }

        public DateTime StartAt { get; set; }

        public DateTime EndAt { get; set; }

        public bool IsActive { get; set; }
    }
}
