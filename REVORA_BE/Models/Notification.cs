using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    public class Notification
    {
        [Key]
        public Guid NotificationId { get; set; } = Guid.NewGuid();

        [Required]
        public long UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = null!; // "buy", "comment", "credit", "like", "follow", "view", "system"

        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = null!;

        [Required]
        public string Message { get; set; } = null!;

        [MaxLength(100)]
        public string? ReferenceId { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ScheduledAt { get; set; }
        
        public bool IsSent { get; set; } = true;
    }
}
