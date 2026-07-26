using System;
using REVORA_BE.Models.Enums;

namespace REVORA_BE.DTOs.Response
{
    public class OrderTransactionResponseDto
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string TransactionCode { get; set; } = null!;

        public string PackageName { get; set; } = null!;
        public int CreditTypeId { get; set; }
        public string CreditTypeName { get; set; } = null!;
        public string CreditTypeDisplayName { get; set; } = null!;

        public PaymentStatus PaymentStatus { get; set; }
        public string PaymentStatusLabel { get; set; } = null!;
        public OrderStatus OrderStatus { get; set; }

        public DateTime TransactionAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        /// <summary>Số tiền cần thanh toán.</summary>
        public decimal ExpectedAmount { get; set; }

        /// <summary>Số tiền thực nhận (null nếu chưa callback).</summary>
        public decimal? ReceivedAmount { get; set; }

        public bool CreditsGranted { get; set; }

        /// <summary>Credit được cộng — 0 nếu thiếu tiền (đớp, không hoàn).</summary>
        public int CreditAmount { get; set; }
    }
}
