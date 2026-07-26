using System;
using System.Collections.Generic;

namespace REVORA_BE.DTOs.CreditPackage
{
    public class PendingOrderInfoDto
    {
        public int PackageId { get; set; }
        public string OrderCode { get; set; } = null!;
        public string CheckoutUrl { get; set; } = null!;
        public DateTime ExpiredAt { get; set; }
    }

    /// <summary>
    /// Ví credit theo loại (Posting / Featured) — FE dùng summary để lock shop, batches để hiển thị chi tiết.
    /// </summary>
    public class MyCreditsByTypeResponseDto
    {
        public int CreditTypeId { get; set; }
        public string? CreditTypeName { get; set; }

        public int TotalRemainingCredits { get; set; }
        public int PaidRemainingCredits { get; set; }
        public int FreeRemainingCredits { get; set; }

        /// <summary>Đang có gói paid còn credit (chặn mua paid mới cùng loại).</summary>
        public bool HasActivePaidCredits { get; set; }

        public List<PendingOrderInfoDto> PendingOrders { get; set; } = new();

        /// <summary>Có thể mua gói paid mới (không còn paid active và không kẹt đơn pending).</summary>
        public bool CanPurchasePaidPackage { get; set; }

        public string? PurchaseBlockReason { get; set; }

        public List<UserCreditBatchResponseDto> Batches { get; set; } = new();
    }
}
