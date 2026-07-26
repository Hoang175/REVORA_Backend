using REVORA_BE.Models;
using REVORA_BE.Repositories.Interfaces;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Implementations
{
    public class AdminAuditLogRepository : IAdminAuditLogRepository
    {
        private readonly AppDbContext _context;

        public AdminAuditLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(AdminAuditLog log)
        {
            await _context.AdminAuditLogs.AddAsync(log);
        }
    }
}
