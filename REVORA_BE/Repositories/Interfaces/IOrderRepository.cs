using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetUserOrdersAsync(long userId, PaymentStatus? paymentStatus = null);
    }
}
