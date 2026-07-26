using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("AdminAuditLogs")]
    public class AdminAuditLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LogId { get; set; }

        public long AdminId { get; set; }

        public long TargetUserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = null!;

        [MaxLength(500)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        [ForeignKey("AdminId")]
        public virtual User? Admin { get; set; }

        [ForeignKey("TargetUserId")]
        public virtual User? TargetUser { get; set; }
    }
}
