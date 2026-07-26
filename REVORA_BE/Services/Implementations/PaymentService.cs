using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql; // Thay thế SqlClient cũ để bắt lỗi chuẩn Postgres
using PayOS.Models.Webhooks;
using REVORA_BE.Data;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Helpers;
using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using REVORA_BE.Repositories.Interfaces;
using REVORA_BE.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly IOrderRepository _orderRepository;
        private readonly IPayOSService _payOSService;
        private readonly ILogger<PaymentService> _logger;
        private readonly INotificationService _notificationService;

        public PaymentService(
            AppDbContext context,
            IOrderRepository orderRepository,
            IPayOSService payOSService,
            ILogger<PaymentService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _orderRepository = orderRepository;
            _payOSService = payOSService;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<object> CheckoutPackageAsync(long userId, long packageId)
        {
            var package = await _context.PaidCreditPackages.FindAsync((int)packageId);

            if (package == null || !package.IsActive)
                throw new Exception("Gói Credit không tồn tại hoặc đã ngưng hoạt động.");

            var currentTime = DateTime.UtcNow;

            var existingPendingOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.UserId == userId
                    && o.PaidCreditPackageId == (int)packageId
                    && o.PaymentStatus == PaymentStatus.Pending
                    && o.ExpiredAt > currentTime);

            if (existingPendingOrder != null)
            {
                return new
                {
                    OrderId = existingPendingOrder.OrderId,
                    OrderCode = existingPendingOrder.OrderCode,
                    PayOSOrderCode = existingPendingOrder.PayOSOrderCode,
                    PaymentUrl = existingPendingOrder.CheckoutUrl,
                    ExpiredAt = existingPendingOrder.ExpiredAt,
                    Amount = existingPendingOrder.AmountPaid,
                    IsExisting = true
                };
            }

            var newOrder = new Order
            {
                UserId = userId,
                PaidCreditPackageId = (int)packageId,
                OrderCode = "REV_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + userId,
                PaymentContent = $"Mua gói {package.Name}",
                PaymentMethod = PaymentMethod.PayOS,
                PaymentStatus = PaymentStatus.Pending,
                Status = OrderStatus.PendingPayment,
                AmountPaid = package.DiscountedPrice,
                CreditsGranted = false,
                CreatedAt = currentTime,
                ExpiredAt = currentTime.AddMinutes(15)
            };

            bool success = false;
            int retryCount = 0;

            while (!success && retryCount < 5)
            {
                try
                {
                    string timePrefix = DateTime.UtcNow.ToString("yyMMddHHmmss");
                    string randomSuffix = Random.Shared.Next(100, 999).ToString();
                    newOrder.PayOSOrderCode = long.Parse(timePrefix + randomSuffix);

                    _context.Orders.Add(newOrder);
                    await _context.SaveChangesAsync();
                    success = true;
                }
                catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("IX_Orders_PayOSOrderCode"))
                {
                    _context.Entry(newOrder).State = EntityState.Detached; // reset state
                    retryCount++;
                }
            }

            if (!success)
            {
                throw new Exception("Không thể tạo đơn hàng do lỗi hệ thống (PayOSOrderCode collision).");
            }

            try
            {
                // Gọi PayOS SDK thật
                var payOsResult = await _payOSService.CreatePaymentLinkAsync(newOrder);
                newOrder.CheckoutUrl = payOsResult.CheckoutUrl;

                // Cập nhật lại CheckoutUrl vào DB
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                newOrder.PaymentStatus = PaymentStatus.Failed;
                newOrder.Status = OrderStatus.Cancelled;
                newOrder.PaymentContent = $"Lỗi tạo cổng thanh toán: {ex.Message}";
                await _context.SaveChangesAsync();

                throw new Exception("Không thể kết nối đến cổng thanh toán PayOS. Vui lòng thử lại sau.", ex);
            }

            return new
            {
                OrderId = newOrder.OrderId,
                OrderCode = newOrder.OrderCode,
                PayOSOrderCode = newOrder.PayOSOrderCode,
                PaymentUrl = newOrder.CheckoutUrl,
                ExpiredAt = newOrder.ExpiredAt,
                Amount = newOrder.AmountPaid,
                IsExisting = false
            };
        }

        /// <summary>
        /// Mock webhook PayOS. Bọc Execution Strategy phòng trường hợp test local bật retry strategy.
        /// </summary>
        public async Task<object> ProcessMockWebhookAsync(long orderId, decimal receivedAmount)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var currentTime = DateTime.UtcNow;
                using var transaction = await _context.Database.BeginTransactionAsync();

                var order = await _context.Orders
                    .Include(o => o.PaidCreditPackage)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    throw new Exception("Không tìm thấy Order.");

                if (order.CreditsGranted)
                    return (object)new { success = true, message = "Order đã được cấp credit (Duplicate Webhook)" };

                if (order.PaymentStatus == PaymentStatus.Successful)
                    return (object)new { success = true, message = "Order đã được xử lý thành công (Duplicate Webhook)" };

                if (order.PaymentStatus != PaymentStatus.Pending)
                    throw new Exception("Order không ở trạng thái chờ thanh toán.");

                if (order.ExpiredAt < currentTime)
                {
                    order.PaymentStatus = PaymentStatus.Expired;
                    order.Status = OrderStatus.Cancelled;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    throw new Exception("Order đã hết hạn.");
                }

                order.ReceivedAmount = receivedAmount;
                order.PaidAt = currentTime;
                order.ProviderTransactionId ??= $"TX{order.OrderId:D3}";
                order.ResponseCode = "00";

                var package = order.PaidCreditPackage;
                var creditsGranted = false;

                if (receivedAmount >= order.AmountPaid)
                {
                    order.PaymentStatus = PaymentStatus.Successful;
                    order.Status = OrderStatus.Completed;
                    order.ResponsePaymentContent = "Giao dịch thành công (MOCK)";

                    if (package != null)
                    {
                        _context.UserCreditBatches.Add(new UserCreditBatch
                        {
                            UserId = order.UserId,
                            CreditTypeId = package.CreditTypeId,
                            PaidPackageId = package.PaidCreditPackageId,
                            RemainingCredits = package.CreditAmount,
                            PurchasedAt = currentTime,
                            ExpiresAt = package.DurationDays.HasValue ? currentTime.AddDays(package.DurationDays.Value) : null,
                            IsActive = true
                        });
                        creditsGranted = true;

                        await _notificationService.CreateNotificationAsync(
                            userId: order.UserId,
                            type: "credit",
                            title: "Nạp credit thành công",
                            message: $"Bạn vừa nạp thành công {package.CreditAmount} credit gói {package.Name}.",
                            referenceId: order.OrderId.ToString()
                        );
                    }
                }
                else
                {
                    order.PaymentStatus = PaymentStatus.Failed;
                    order.Status = OrderStatus.Cancelled;
                    order.ResponsePaymentContent = "Nhận thiếu tiền — không cộng credit (không nạp bù)";
                }

                order.CreditsGranted = creditsGranted;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new
                {
                    order.OrderId,
                    PaymentStatus = order.PaymentStatus.ToString(),
                    PaymentStatusLabel = GetPaymentStatusLabel(order.PaymentStatus),
                    order.AmountPaid,
                    order.ReceivedAmount,
                    order.CreditsGranted,
                    CreditAmount = creditsGranted ? package?.CreditAmount ?? 0 : 0,
                    IsUnderpaid = receivedAmount < order.AmountPaid
                };
            });
        }

        public async Task<decimal> GetOrderExpectedAmountAsync(long orderId)
        {
            var order = await _context.Orders.FindAsync(orderId)
                ?? throw new Exception("Không tìm thấy Order.");
            return order.AmountPaid;
        }

        public async Task<IEnumerable<OrderTransactionResponseDto>> GetMyTransactionsAsync(
            long userId, PaymentStatus? paymentStatus = null)
        {
            var orders = await _orderRepository.GetUserOrdersAsync(userId, paymentStatus);
            return orders.Select(MapToTransactionDto).ToList();
        }

        private static OrderTransactionResponseDto MapToTransactionDto(Order order)
        {
            var package = order.PaidCreditPackage;
            var creditTypeName = package?.CreditType?.Name ?? string.Empty;
            var creditsGranted = order.CreditsGranted;

            return new OrderTransactionResponseDto
            {
                OrderId = order.OrderId,
                OrderCode = order.OrderCode,
                TransactionCode = order.ProviderTransactionId ?? order.OrderCode,
                PackageName = package?.Name ?? string.Empty,
                CreditTypeId = package?.CreditTypeId ?? 0,
                CreditTypeName = creditTypeName,
                CreditTypeDisplayName = PaymentDisplayHelper.GetCreditTypeDisplayName(creditTypeName),
                PaymentStatus = order.PaymentStatus,
                PaymentStatusLabel = GetPaymentStatusLabel(order.PaymentStatus),
                OrderStatus = order.Status,
                TransactionAt = order.PaidAt ?? order.CreatedAt,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                PaymentMethod = order.PaymentMethod,
                ExpectedAmount = order.AmountPaid,
                ReceivedAmount = order.ReceivedAmount,
                CreditsGranted = creditsGranted,
                CreditAmount = creditsGranted ? package?.CreditAmount ?? 0 : 0
            };
        }

        private static string GetPaymentStatusLabel(PaymentStatus status) => status switch
        {
            PaymentStatus.Pending => "Chờ thanh toán",
            PaymentStatus.Successful => "Thành công",
            PaymentStatus.Failed => "Thất bại",
            PaymentStatus.Expired => "Hết hạn",
            PaymentStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };

        public async Task<PaymentStatusResponseDto> GetPaymentStatusAsync(string orderCode)
        {
            var order = await _context.Orders
                .Include(o => o.PaidCreditPackage)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

            if (order == null)
            {
                throw new Exception("Không tìm thấy Order.");
            }

            if (order.PaymentStatus == PaymentStatus.Pending && order.ExpiredAt < DateTime.UtcNow)
            {
                order.PaymentStatus = PaymentStatus.Expired;
                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync();
            }

            return new PaymentStatusResponseDto
            {
                OrderCode = order.OrderCode,
                Status = order.Status,
                StatusName = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus,
                PaymentStatusName = order.PaymentStatus.ToString(),
                Amount = order.AmountPaid,
                PackageName = order.PaidCreditPackage?.Name,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                ExpiredAt = order.ExpiredAt
            };
        }

        /// <summary>
        /// XỬ LÝ WEBHOOK CHUẨN TỪ PAYOS ĐÃ FIX LỖI EXECUTION STRATEGY VÀ POSTGRESQL EXCEPTION
        /// </summary>
        public async Task<object> ProcessPayOSWebhookAsync(WebhookData data)
        {
            // Bọc toàn bộ vào Execution Strategy của Postgres chống crash transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                var currentTime = DateTime.UtcNow;
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var order = await _context.Orders
                        .Include(o => o.PaidCreditPackage)
                        .FirstOrDefaultAsync(o => o.PayOSOrderCode == data.OrderCode);

                    if (order == null)
                    {
                        _logger.LogWarning($"[Webhook] Không tìm thấy Order với PayOSOrderCode = {data.OrderCode}. Bỏ qua webhook.");
                        return new { success = true, reason = "order_not_found" };
                    }

                    if (order.CreditsGranted || order.PaymentStatus == PaymentStatus.Successful)
                    {
                        _logger.LogInformation($"[Webhook] Giao dịch {data.OrderCode} đã được xử lý trước đó. Bỏ qua webhook.");
                        return new { success = true, reason = "already_processed" };
                    }

                    var package = order.PaidCreditPackage;
                    if (package == null)
                    {
                        _logger.LogError($"[Webhook] Order {order.OrderId} không có gói PaidCreditPackage đính kèm.");
                        throw new Exception("Lỗi dữ liệu hệ thống: Không tìm thấy gói Credit đính kèm với đơn hàng.");
                    }

                    if (order.ExpiredAt < currentTime)
                    {
                        _logger.LogInformation($"[Webhook] Giao dịch {data.OrderCode} thanh toán thành công nhưng bị trễ (sau {order.ExpiredAt}). Tiến hành cấp phát Credit.");
                    }

                    order.ReceivedAmount = data.Amount;
                    order.ProviderTransactionId = data.Reference;
                    order.ResponseCode = data.Code;

                    bool creditsGranted = false;

                    if (data.Code != "00")
                    {
                        _logger.LogWarning($"[Webhook] Giao dịch {data.OrderCode} thất bại từ phía ngân hàng (Code: {data.Code}).");
                        order.PaymentStatus = PaymentStatus.Failed;
                        order.Status = OrderStatus.Cancelled;
                        order.ResponsePaymentContent = $"Giao dịch thất bại (Code: {data.Code})";
                    }
                    else if (data.Amount < order.AmountPaid)
                    {
                        _logger.LogWarning($"[Webhook] Underpayment cho {data.OrderCode}: nhận {data.Amount}, cần {order.AmountPaid}.");
                        order.PaymentStatus = PaymentStatus.Failed;
                        order.Status = OrderStatus.Cancelled;
                        order.ResponsePaymentContent = $"Thanh toán thiếu ({data.Amount} / {order.AmountPaid}). Không cộng Credit.";
                    }
                    else
                    {
                        _logger.LogInformation($"[Webhook] Giao dịch {data.OrderCode} thành công. Cấp phát Credit.");
                        order.PaymentStatus = PaymentStatus.Successful;
                        order.Status = OrderStatus.Completed;
                        order.ResponsePaymentContent = "Giao dịch thành công qua PayOS";
                        order.PaidAt = currentTime;

                        _context.UserCreditBatches.Add(new UserCreditBatch
                        {
                            UserId = order.UserId,
                            CreditTypeId = package.CreditTypeId,
                            PaidPackageId = package.PaidCreditPackageId,
                            OrderId = order.OrderId, // Chống trùng lặp qua Unique Index
                            RemainingCredits = package.CreditAmount,
                            PurchasedAt = currentTime,
                            ExpiresAt = package.DurationDays.HasValue ? currentTime.AddDays(package.DurationDays.Value) : null,
                            IsActive = true
                        });
                        creditsGranted = true;

                        // --- UNLOCK BADGE LOGIC ---
                        if (package.RewardBadgeId.HasValue)
                        {
                            var existingBadge = await _context.UserBadges.FirstOrDefaultAsync(ub => ub.UserId == order.UserId && ub.BadgeId == package.RewardBadgeId.Value);
                            
                            DateTime? newExpiredAt = package.BadgeDurationDays.HasValue ? currentTime.AddDays(package.BadgeDurationDays.Value) : null;

                            if (existingBadge == null)
                            {
                                _context.UserBadges.Add(new UserBadge
                                {
                                    UserId = order.UserId,
                                    BadgeId = package.RewardBadgeId.Value,
                                    ExpiredAt = newExpiredAt
                                });

                                var userToUpdate = await _context.Users.FindAsync(order.UserId);
                                if (userToUpdate != null && !userToUpdate.BadgeId.HasValue)
                                {
                                    userToUpdate.BadgeId = package.RewardBadgeId.Value;
                                }
                            }
                            else
                            {
                                if (package.BadgeDurationDays.HasValue)
                                {
                                    if (existingBadge.ExpiredAt.HasValue && existingBadge.ExpiredAt.Value > currentTime)
                                    {
                                        existingBadge.ExpiredAt = existingBadge.ExpiredAt.Value.AddDays(package.BadgeDurationDays.Value);
                                    }
                                    else
                                    {
                                        existingBadge.ExpiredAt = newExpiredAt;
                                    }
                                }
                                else
                                {
                                    existingBadge.ExpiredAt = null;
                                }
                            }
                        }
                        // --- END UNLOCK BADGE LOGIC ---

                        await _notificationService.CreateNotificationAsync(
                            userId: order.UserId,
                            type: "credit",
                            title: "Nạp credit thành công",
                            message: $"Bạn vừa nạp thành công {package.CreditAmount} credit gói {package.Name}.",
                            referenceId: order.OrderId.ToString()
                        );
                    }

                    order.CreditsGranted = creditsGranted;
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new { success = true, reason = "processed_successfully" };
                }
                // FIX CHUẨN POSTGRESQL: Bắt mã lỗi 23505 (Unique Violation) thay thế cho mã lỗi SqlServer cũ
                catch (DbUpdateException dbEx) when (dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
                {
                    _logger.LogWarning($"[Webhook] Phát hiện xử lý đồng thời (Duplicate UserCreditBatch) cho OrderId {data.OrderCode}. Đã rollback an toàn.");
                    await transaction.RollbackAsync();
                    return new { success = true, reason = "duplicate" };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[Webhook] Lỗi không xác định khi xử lý webhook cho PayOSOrderCode {data.OrderCode}.");
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<bool> CancelOrderAsync(string orderCode, long userId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode && o.UserId == userId);

            if (order == null || order.PaymentStatus != PaymentStatus.Pending)
                return false;

            order.PaymentStatus = PaymentStatus.Cancelled;
            order.Status = OrderStatus.Cancelled;
            order.PaymentContent = "Người dùng chủ động hủy giao dịch trên PayOS";

            await _context.SaveChangesAsync();
            return true;
        }
    }
}