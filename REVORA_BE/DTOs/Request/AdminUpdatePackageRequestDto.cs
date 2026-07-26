using System.Collections.Generic;

namespace REVORA_BE.DTOs.Request
{
    public class AdminUpdatePackageRequestDto
    {
        public string Name { get; set; } = null!;
        public decimal OriginalPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal DiscountedPrice { get; set; }
        public bool IsActive { get; set; }
        public int? RewardBadgeId { get; set; }
        public List<string> Descriptions { get; set; } = new List<string>();
    }
}
