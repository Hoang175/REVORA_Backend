using Microsoft.EntityFrameworkCore;
using REVORA_BE.Data;
using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using REVORA_BE.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class CreditPurchaseValidationService : ICreditPurchaseValidationService
    {
        public const string ActivePaidCreditsMessage =
            "Bạn đang có gói trả phí cùng loại còn credit. Hãy dùng hết credit trước khi mua gói mới.";

        private readonly AppDbContext _context;

        public CreditPurchaseValidationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetActivePendingOrdersForCreditTypeAsync(long userId, int creditTypeId)
        {
            var now = DateTime.UtcNow;
            return await _context.Orders
                .Include(o => o.PaidCreditPackage)
                .Where(o => o.UserId == userId
                    && o.PaymentStatus == PaymentStatus.Pending
                    && o.ExpiredAt > now
                    && o.PaidCreditPackage != null
                    && o.PaidCreditPackage.CreditTypeId == creditTypeId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public Task<(bool CanPurchase, string? BlockReason)> GetPurchaseStatusAsync(
            long userId, int creditTypeId, int packageId)
        {
            // We no longer block purchasing other packages if there's a pending order.
            return Task.FromResult<(bool, string?)>((true, null));
        }

        public Task<(bool CanPurchasePaidPackage, string? BlockReason)>
            GetPaidPurchaseStatusForCreditTypeAsync(long userId, int creditTypeId)
        {
            return Task.FromResult<(bool, string?)>((true, null));
        }

        public Task ValidateCheckoutAsync(long userId, PaidCreditPackage package)
        {
            // No longer throwing exceptions for pending orders of other packages.
            return Task.CompletedTask;
        }
    }
}
