namespace REVORA_BE.DTOs.Request
{
    /// <summary>Mock webhook PayOS — mô phỏng số tiền user chuyển thực tế.</summary>
    public class MockPaymentWebhookRequest
    {
        /// <summary>Bỏ trống = chuyển đủ ExpectedAmount.</summary>
        public decimal? ReceivedAmount { get; set; }
    }
}
