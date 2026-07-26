using REVORA_BE.DTOs.Response;
using REVORA_BE.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<object> CheckoutPackageAsync(long userId, long packageId);
        Task<object> ProcessMockWebhookAsync(long orderId, decimal receivedAmount);
        Task<decimal> GetOrderExpectedAmountAsync(long orderId);
        Task<IEnumerable<OrderTransactionResponseDto>> GetMyTransactionsAsync(long userId, PaymentStatus? paymentStatus = null);
        Task<PaymentStatusResponseDto> GetPaymentStatusAsync(string orderCode);
        Task<object> ProcessPayOSWebhookAsync(PayOS.Models.Webhooks.WebhookData data);
        Task<bool> CancelOrderAsync(string orderCode, long userId);
    }
}