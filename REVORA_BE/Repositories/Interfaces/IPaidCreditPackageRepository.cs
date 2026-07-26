using REVORA_BE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Interfaces
{
    public interface IPaidCreditPackageRepository
    {
        Task<IEnumerable<PaidCreditPackage>> GetAllActivePackagesAsync();
        Task<PaidCreditPackage?> GetByIdAsync(long id);
        Task UpdateAsync(PaidCreditPackage entity);
    }
}