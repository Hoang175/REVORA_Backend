using Microsoft.EntityFrameworkCore;
using REVORA_BE.Data;
using REVORA_BE.Models;
using REVORA_BE.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Implementations
{
    public class PaidCreditPackageRepository : IPaidCreditPackageRepository
    {
        private readonly AppDbContext _context;

        public PaidCreditPackageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PaidCreditPackage>> GetAllActivePackagesAsync()
        {
            return await _context.PaidCreditPackages
                .Include(p => p.CreditType)
                .Include(p => p.RewardBadge)
                .Include(p => p.Descriptions)
                .Where(p => p.IsActive)
                .ToListAsync();
        }

        public async Task<PaidCreditPackage?> GetByIdAsync(long id)
        {
            return await _context.PaidCreditPackages
                .Include(p => p.CreditType)
                .Include(p => p.RewardBadge)
                .Include(p => p.Descriptions)
                .FirstOrDefaultAsync(p => p.PaidCreditPackageId == id);
        }

        public async Task UpdateAsync(PaidCreditPackage entity)
        {
            _context.PaidCreditPackages.Update(entity);
            await _context.SaveChangesAsync();
        }
    }
}