using REVORA_BE.DTOs.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IPaidCreditPackageService
    {
        Task<IEnumerable<PaidCreditPackageResponseDto>> GetAllActivePackagesAsync();
        Task<PaidCreditPackageResponseDto?> GetPackageByIdAsync(long id);
        Task<bool> UpdatePackageAsync(long id, REVORA_BE.DTOs.Request.AdminUpdatePackageRequestDto request);
    }
}