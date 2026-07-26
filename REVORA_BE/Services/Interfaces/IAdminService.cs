using REVORA_BE.DTOs.Response;
using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IAdminService
    {
        Task<List<AdminProductResponseDto>> GetAllProductsAsync();
        Task<bool> UpdateProductStatusAsync(long productId, string status, string? note = null);
        Task<AdminRevenueResponseDto> GetRevenueStatsAsync(string filterType, int? year, int? month, DateTime? startDate, DateTime? endDate);
        Task<AdminDashboardResponseDto> GetDashboardStatsAsync(string timeRange = "week");
        Task<int> SendNotificationsAsync(AdminSendNotificationRequestDto request);
        Task<List<UserSearchDto>> SearchUsersAsync(string query);
        Task<PagedResult<AdminUserResponseDto>> GetUsersAsync(AdminUserQueryDto query);
        Task<AdminUsersSummaryDto> GetUsersSummaryAsync();
        Task<bool> ToggleUserStatusAsync(long userId, ToggleUserStatusDto request, long currentAdminId);
        Task<PagedResult<TransactionResponseDto>> GetUserTransactionsAsync(long userId, int page, int pageSize);
        Task<AdminUserOverviewDto> GetUserOverviewAsync(long userId);
        Task<List<REVORA_BE.DTOs.Response.BadgeResponseDto>> GetBadgesAsync();
    }
}
