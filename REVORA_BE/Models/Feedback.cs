using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    public class Feedback
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long FeedbackId { get; set; }

        public long? UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "New"; // "New", "Reviewed"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
