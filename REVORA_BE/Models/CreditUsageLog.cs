using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    public class CreditUsageLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long LogId { get; set; }

        public long UserId { get; set; }

        public int CreditTypeId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ActionType { get; set; } // "post_new", "renew", "boost_featured", "extend_featured"

        public int Amount { get; set; }

        [MaxLength(255)]
        public string ProductName { get; set; }

        public long? ProductId { get; set; }

        public int BalanceAfter { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("CreditTypeId")]
        public virtual CreditType CreditType { get; set; }
    }
}
