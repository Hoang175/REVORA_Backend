namespace REVORA_BE.DTOs.Response
{
    public class PaidCreditPackageResponseDto
    {
        public long PaidCreditPackageId { get; set; }
        public string Name { get; set; } = null!;
        public long CreditTypeId { get; set; }
        public string? CreditTypeName { get; set; } 
        public int CreditAmount { get; set; }
        public int? DurationDays { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal DiscountedPrice { get; set; }
        public int? RewardBadgeId { get; set; }
        public BadgeResponseDto? RewardBadge { get; set; }
        public bool IsActive { get; set; }
        public List<string> Descriptions { get; set; } = new();

    }
}