using REVORA_BE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Interfaces
{
    public interface IUserCreditBatchRepository
    {
        Task<IEnumerable<UserCreditBatch>> GetActiveBatchesByUserIdAndTypeAsync(long userId, long creditTypeId);
    }
}