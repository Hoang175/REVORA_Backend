using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using REVORA_BE.Models;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IPayOSService
    {
        Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(Order order);
        Task<WebhookData> VerifyWebhookDataAsync(Webhook webhookBody);
    }
}
