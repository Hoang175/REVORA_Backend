using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("PaidCreditPackage")]
    public class PaidCreditPackage
    {
        public int PaidCreditPackageId { get; set; }

        public int CreditTypeId { get; set; }

        public string Name { get; set; } = null!; // const

        public int CreditAmount { get; set; }

        public int? DurationDays { get; set; } // const, null = vĩnh viễn

        public decimal OriginalPrice { get; set; }

        public decimal DiscountRate { get; set; }

        public decimal DiscountedPrice { get; set; }

        public int? RewardBadgeId { get; set; } // Link trực tiếp tới phần thưởng là Badge

        public int? BadgeDurationDays { get; set; }

        public bool IsActive { get; set; }

        public CreditType? CreditType { get; set; }

        [ForeignKey("RewardBadgeId")]
        public Badge? RewardBadge { get; set; }

        public ICollection<UserCreditBatch> UserCreditBatches { get; set; } = new HashSet<UserCreditBatch>();

        public ICollection<Order> Orders { get; set; } = new HashSet<Order>();

        public ICollection<PaidCreditPackageDescription> Descriptions { get; set; } = new HashSet<PaidCreditPackageDescription>();
    }
}
