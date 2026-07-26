using REVORA_BE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface ICreditPurchaseValidationService
    {
        Task<List<Order>> GetActivePendingOrdersForCreditTypeAsync(long userId, int creditTypeId);

        Task<(bool CanPurchase, string? BlockReason)> GetPurchaseStatusAsync(long userId, int creditTypeId, int packageId);

        Task<(bool CanPurchasePaidPackage, string? BlockReason)> GetPaidPurchaseStatusForCreditTypeAsync(long userId, int creditTypeId);

        Task ValidateCheckoutAsync(long userId, PaidCreditPackage package);
    }
}
