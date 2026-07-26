using REVORA_BE.Models.Enums;

namespace REVORA_BE.DTOs.Response
{
    public class PaymentStatusResponseDto
    {
        public string OrderCode { get; set; } = null!;
        public OrderStatus Status { get; set; }
        public string StatusName { get; set; } = null!;
        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentStatusName { get; set; } = null!;
        public decimal Amount { get; set; }
        public string? PackageName { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public System.DateTime? PaidAt { get; set; }
        public System.DateTime? ExpiredAt { get; set; }
    }
}
