using System;
using System.ComponentModel.DataAnnotations.Schema;
using REVORA_BE.Models.Enums;

namespace REVORA_BE.Models
{
    [Table("Orders")]
    public class Order
    {
        public long OrderId { get; set; }

        // REVORA20260601-0001
        public string OrderCode { get; set; } = null!;

        // Khóa riêng dùng cho PayOS (long, max 53 bit)
        public long PayOSOrderCode { get; set; }


        public long UserId { get; set; }

        public int PaidCreditPackageId { get; set; }

        // PAYOS
        public PaymentMethod PaymentMethod { get; set; }

        // Pending / Successful / Expired
        public PaymentStatus PaymentStatus { get; set; }

        public OrderStatus Status { get; set; }

        // URL thanh toán (dùng để Resume giao dịch)
        public string? CheckoutUrl { get; set; }

        // Nội dung chuyển khoản
        public string PaymentContent { get; set; } = null!;

        /// <summary>Số tiền cần thanh toán (giá gói lúc tạo order).</summary>
        public decimal AmountPaid { get; set; }

        /// <summary>Số tiền thực nhận từ PayOS (null khi chưa có callback).</summary>
        public decimal? ReceivedAmount { get; set; }

        /// <summary>Đã cộng credit cho user hay chưa (chỉ true khi Successful và đủ/thừa tiền).</summary>
        public bool CreditsGranted { get; set; }

        // Mã giao dịch phía PayOS
        public string? ProviderTransactionId { get; set; }

        // Mã phản hồi PayOS
        public string? ResponseCode { get; set; }

        // Nội dung callback trả về
        public string? ResponsePaymentContent { get; set; }

        // Thời gian tạo order
        public DateTime CreatedAt { get; set; }

        // Hết hạn QR
        public DateTime ExpiredAt { get; set; }

        // Thanh toán thành công
        public DateTime? PaidAt { get; set; }

        public User? User { get; set; }

        public PaidCreditPackage? PaidCreditPackage { get; set; }
    }
}