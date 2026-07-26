using REVORA_BE.Models;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Interfaces
{
    public interface IAdminAuditLogRepository
    {
        Task AddAsync(AdminAuditLog log);
    }
}
