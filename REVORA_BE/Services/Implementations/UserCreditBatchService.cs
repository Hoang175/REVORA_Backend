using REVORA_BE.DTOs.CreditPackage;
using REVORA_BE.Models;
using REVORA_BE.Repositories.Interfaces;
using REVORA_BE.Services.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace REVORA_BE.Services.Implementations
{
    public class UserCreditBatchService : IUserCreditBatchService
    {
        private readonly IUserCreditBatchRepository _repository;
        private readonly ICreditPurchaseValidationService _purchaseValidation;
        private readonly AppDbContext _context;

        public UserCreditBatchService(
            IUserCreditBatchRepository repository,
            ICreditPurchaseValidationService purchaseValidation,
            AppDbContext context)
        {
            _repository = repository;
            _purchaseValidation = purchaseValidation;
            _context = context;
        }

        public async Task<MyCreditsByTypeResponseDto> GetMyCreditsByTypeAsync(long userId, long creditTypeId)
        {
            var batches = (await _repository.GetActiveBatchesByUserIdAndTypeAsync(userId, creditTypeId)).ToList();
            var batchDtos = batches.Select(MapBatch).ToList();

            var pendingOrders = await _purchaseValidation.GetActivePendingOrdersForCreditTypeAsync(userId, (int)creditTypeId);

            var paidBatches = batches.Where(b => b.PaidPackageId != null).ToList();
            var freeBatches = batches.Where(b => b.FreePackageId != null).ToList();

            return new MyCreditsByTypeResponseDto
            {
                CreditTypeId = (int)creditTypeId,
                CreditTypeName = batches.FirstOrDefault()?.CreditType?.Name,
                TotalRemainingCredits = batches.Sum(b => b.RemainingCredits),
                PaidRemainingCredits = paidBatches.Sum(b => b.RemainingCredits),
                FreeRemainingCredits = freeBatches.Sum(b => b.RemainingCredits),
                HasActivePaidCredits = paidBatches.Any(b => b.RemainingCredits > 0),
                PendingOrders = pendingOrders.Select(po => new PendingOrderInfoDto
                {
                    PackageId = po.PaidCreditPackageId,
                    OrderCode = po.OrderCode,
                    CheckoutUrl = po.CheckoutUrl,
                    ExpiredAt = po.ExpiredAt
                }).ToList(),
                CanPurchasePaidPackage = true,
                PurchaseBlockReason = null,
                Batches = batchDtos
            };
        }

        private static UserCreditBatchResponseDto MapBatch(UserCreditBatch b)
        {
            var isPaid = b.PaidPackageId != null;
            return new UserCreditBatchResponseDto
            {
                UserCreditBatchId = b.BatchId,
                CreditTypeId = b.CreditTypeId,
                CreditTypeName = b.CreditType?.Name,
                RemainingCredits = b.RemainingCredits,
                ExpiresAt = b.ExpiresAt,
                IsPaid = isPaid,
                PackageId = isPaid ? b.PaidPackageId!.Value : b.FreePackageId!.Value,
                PackageName = b.PaidCreditPackage?.Name ?? b.FreeCreditPackage?.Name
            };
        }
        
        public async Task<List<REVORA_BE.DTOs.Response.CreditUsageLogResponseDto>> GetMyUsageHistoryAsync(long userId)
        {
            var logs = await _context.CreditUsageLogs
                .Include(l => l.CreditType)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return logs.Select(l => new REVORA_BE.DTOs.Response.CreditUsageLogResponseDto
            {
                Id = "CU" + l.LogId.ToString("D3"),
                Date = l.CreatedAt.AddHours(7).ToString("dd/MM/yyyy"),
                Time = l.CreatedAt.AddHours(7).ToString("HH:mm"),
                Action = l.ActionType,
                CreditType = l.CreditType?.Name == "Posting" ? "posting" : "featured",
                Amount = l.Amount,
                ProductName = l.ProductName,
                ProductId = l.ProductId?.ToString(),
                BalanceAfter = l.BalanceAfter
            }).ToList();
        }
    }
}
