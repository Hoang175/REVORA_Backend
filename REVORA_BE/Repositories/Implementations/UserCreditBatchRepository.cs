using Microsoft.EntityFrameworkCore;
using REVORA_BE.Data;
using REVORA_BE.Models;
using REVORA_BE.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Implementations
{
    public class UserCreditBatchRepository : IUserCreditBatchRepository
    {
        private readonly AppDbContext _context;

        public UserCreditBatchRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<UserCreditBatch>> GetActiveBatchesByUserIdAndTypeAsync(long userId, long creditTypeId)
        {
            return await _context.UserCreditBatches
                .Include(u => u.CreditType)
                .Include(u => u.FreeCreditPackage)
                .Include(u => u.PaidCreditPackage)
                .Where(u => u.UserId == userId 
                         && u.CreditTypeId == creditTypeId // Lọc thêm theo loại credit
                         && u.IsActive 
                         && u.RemainingCredits > 0 
                         && (u.ExpiresAt == null || u.ExpiresAt > DateTime.UtcNow))
                .OrderByDescending(u => u.ExpiresAt.HasValue)
                .ThenBy(u => u.ExpiresAt)
                .ToListAsync();
        }
    }
}