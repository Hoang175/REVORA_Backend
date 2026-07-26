using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("FreeCreditPackage")]
    public class FreeCreditPackage
    {
        public int FreeCreditPackageId { get; set; }

        public int CreditTypeId { get; set; }

        public string Name { get; set; } = null!; // const

        public int CreditAmount { get; set; }

        public int DurationDays { get; set; } // const

        public int RewardBadgeId { get; set; } // Link trực tiếp tới phần thưởng là Badge

        public int? BadgeDurationDays { get; set; }

        public bool IsActive { get; set; }

        public CreditType? CreditType { get; set; }

        [ForeignKey("RewardBadgeId")]
        public Badge? RewardBadge { get; set; }
    }
}
