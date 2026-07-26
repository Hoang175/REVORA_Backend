using Microsoft.EntityFrameworkCore;
using REVORA_BE.Data;
using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using REVORA_BE.Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Implementations
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(long userId, PaymentStatus? paymentStatus = null)
        {
            var query = _context.Orders
                .Include(o => o.PaidCreditPackage)
                    .ThenInclude(p => p!.CreditType)
                .Where(o => o.UserId == userId);

            if (paymentStatus.HasValue)
                query = query.Where(o => o.PaymentStatus == paymentStatus.Value);

            return await query
                .OrderByDescending(o => o.PaidAt ?? o.CreatedAt)
                .ToListAsync();
        }
    }
}
