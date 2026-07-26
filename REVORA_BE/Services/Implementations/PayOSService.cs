using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using REVORA_BE.Helpers;
using REVORA_BE.Models;
using REVORA_BE.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class PayOSService : IPayOSService
    {
        private readonly PayOSClient _payOS;
        private readonly PayOSSettings _settings;

        public PayOSService(IOptions<PayOSSettings> options)
        {
            _settings = options.Value;
            _payOS = new PayOSClient(_settings.ClientId, _settings.ApiKey, _settings.ChecksumKey);
        }

        public async Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(Order order)
        {
            var items = new List<PaymentLinkItem>
            {
                new PaymentLinkItem 
                { 
                    Name = "REVORA Credit Package", 
                    Quantity = 1, 
                    Price = (long)order.AmountPaid 
                }
            };

            // PayOS giới hạn Description tối đa 25 ký tự
            string description = order.OrderCode.Length > 25 
                ? order.OrderCode.Substring(0, 25) 
                : order.OrderCode;

            var paymentData = new CreatePaymentLinkRequest
            {
                OrderCode = order.PayOSOrderCode, 
                Amount = (long)order.AmountPaid,
                Description = description,
                Items = items,
                CancelUrl = $"{_settings.CancelUrl}?revoraOrder={Uri.EscapeDataString(order.OrderCode)}",
                ReturnUrl = $"{_settings.ReturnUrl}?revoraOrder={Uri.EscapeDataString(order.OrderCode)}"
            };

            return await _payOS.PaymentRequests.CreateAsync(paymentData);
        }

        public async Task<WebhookData> VerifyWebhookDataAsync(Webhook webhookBody)
        {
            return await _payOS.Webhooks.VerifyAsync(webhookBody);
        }
    }
}
