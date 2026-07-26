using REVORA_BE.DTOs.CreditPackage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IUserCreditBatchService
    {
        Task<MyCreditsByTypeResponseDto> GetMyCreditsByTypeAsync(long userId, long creditTypeId);
        Task<List<REVORA_BE.DTOs.Response.CreditUsageLogResponseDto>> GetMyUsageHistoryAsync(long userId);
    }
}