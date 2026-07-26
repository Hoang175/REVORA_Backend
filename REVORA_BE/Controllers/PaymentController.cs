using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PayOS.Models.Webhooks;
using REVORA_BE.Models.Enums;
using REVORA_BE.DTOs.Request;
using REVORA_BE.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/payment")]
    [ApiController]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IPayOSService _payOSService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            IPayOSService payOSService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _payOSService = payOSService;
            _logger = logger;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
        {
            try
            {
                long userId = GetCurrentUserId();
                var result = await _paymentService.CheckoutPackageAsync(userId, request.PackageId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy mã thanh toán thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetMyTransactions([FromQuery] PaymentStatus? status = null)
        {
            try
            {
                long userId = GetCurrentUserId();
                var transactions = await _paymentService.GetMyTransactionsAsync(userId, status);

                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch sử giao dịch thành công",
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("status/{orderCode}")]
        public async Task<IActionResult> GetPaymentStatus(string orderCode)
        {
            try
            {
                var result = await _paymentService.GetPaymentStatusAsync(orderCode);
                return Ok(new
                {
                    success = true,
                    message = "Lấy trạng thái giao dịch thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("cancel/{orderCode}")]
        public async Task<IActionResult> CancelOrder(string orderCode)
        {
            try
            {
                long userId = GetCurrentUserId();
                var result = await _paymentService.CancelOrderAsync(orderCode, userId);

                return Ok(new
                {
                    success = result,
                    message = result ? "Hủy giao dịch thành công" : "Không thể hủy giao dịch"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("payos-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOSWebhook([FromBody] Webhook webhookBody)
        {
            try
            {
                _logger.LogInformation("[Webhook] Nhận được request webhook từ PayOS.");

                // Verify Signature
                var webhookData = await _payOSService.VerifyWebhookDataAsync(webhookBody);

                // Process the webhook
                var result = await _paymentService.ProcessPayOSWebhookAsync(webhookData);

                // Luôn trả về 200 OK kèm theo reason
                return Ok(result);
            }
            catch (PayOS.Exceptions.InvalidSignatureException ex)
            {
                _logger.LogWarning($"[Webhook] Xác thực chữ ký PayOS thất bại: {ex.Message}");
                return Ok(new { success = true }); // Return 200 as requested
            }
            catch (Exception ex)
            {
                // Nếu lỗi Server -> Báo lỗi để log lại
                _logger.LogError(ex, "[Webhook] Lỗi nội bộ server.");
                return StatusCode(500, new { success = false, message = "Internal Server Error" });
            }
        }

        /// <summary>Mock webhook PayOS. Body.receivedAmount bỏ trống = chuyển đủ tiền.</summary>
        [HttpPost("mock-webhook/{orderId}")]
        public async Task<IActionResult> MockWebhook(long orderId, [FromBody] MockPaymentWebhookRequest? request = null)
        {
            try
            {
                var result = await _paymentService.ProcessMockWebhookAsync(
                    orderId,
                    request?.ReceivedAmount ?? await ResolveFullAmountAsync(orderId));

                return Ok(new
                {
                    success = true,
                    message = "Xử lý webhook (MOCK) thành công",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("mock-success/{orderId}")]
        public async Task<IActionResult> MockSuccess(long orderId)
        {
            try
            {
                var receivedAmount = await ResolveFullAmountAsync(orderId);
                var result = await _paymentService.ProcessMockWebhookAsync(orderId, receivedAmount);
                return Ok(new
                {
                    success = true,
                    message = "Thanh toán đủ tiền (MOCK). Đã cộng credit!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Mock chuyển thiếu tiền — Successful nhưng không cộng credit (đớp).</summary>
        [HttpPost("mock-underpaid/{orderId}")]
        public async Task<IActionResult> MockUnderpaid(long orderId)
        {
            try
            {
                var expected = await ResolveFullAmountAsync(orderId);
                var result = await _paymentService.ProcessMockWebhookAsync(orderId, expected / 2);
                return Ok(new
                {
                    success = true,
                    message = "Nhận thiếu tiền (MOCK). Không cộng credit.",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private async Task<decimal> ResolveFullAmountAsync(long orderId) =>
            await _paymentService.GetOrderExpectedAmountAsync(orderId);

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
            {
                if (Request.Headers.TryGetValue("X-Test-User-Id", out var testUserIdStr) && long.TryParse(testUserIdStr, out long testUserId))
                    userId = testUserId;
                else
                    userId = 3;
            }
            return userId;
        }
    }

    public class CheckoutRequest
    {
        public long PackageId { get; set; }
    }
}
