using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.Services.Interfaces;
using REVORA_BE.DTOs.Request;
using System.Threading.Tasks;
using System;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("Products")]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _adminService.GetAllProductsAsync();
            return Ok(new { success = true, data = products });
        }

        [HttpPut("Products/{id}/status")]
        public async Task<IActionResult> UpdateProductStatus(long id, [FromBody] UpdateProductStatusRequest request)
        {
            var result = await _adminService.UpdateProductStatusAsync(id, request.Status, request.Note);
            if (!result) return NotFound(new { success = false, message = "Không tìm thấy sản phẩm" });
            return Ok(new { success = true, message = "Cập nhật thành công" });
        }

        [HttpGet("Revenue")]
        public async Task<IActionResult> GetRevenueStats(
            [FromQuery] string filterType = "month",
            [FromQuery] int? year = null,
            [FromQuery] int? month = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var stats = await _adminService.GetRevenueStatsAsync(filterType, year, month, startDate, endDate);
            return Ok(new { success = true, data = stats });
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] string timeRange = "week")
        {
            var stats = await _adminService.GetDashboardStatsAsync(timeRange);
            return Ok(new { success = true, data = stats });
        }

        [HttpPost("send-notifications")]
        public async Task<IActionResult> SendNotifications([FromBody] AdminSendNotificationRequestDto request)
        {
            var count = await _adminService.SendNotificationsAsync(request);
            return Ok(new { success = true, count = count, message = $"Đã gửi thông báo đến {count} người dùng." });
        }

        [HttpGet("Users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query)
        {
            var users = await _adminService.SearchUsersAsync(query);
            return Ok(new { success = true, data = users });
        }

        [HttpGet("Users")]
        public async Task<IActionResult> GetUsers([FromQuery] REVORA_BE.DTOs.AdminUserQueryDto query)
        {
            var result = await _adminService.GetUsersAsync(query);
            return Ok(new { success = true, data = result });
        }

        [HttpPatch("Users/{id}/status")]
        public async Task<IActionResult> ToggleUserStatus(long id, [FromBody] REVORA_BE.DTOs.ToggleUserStatusDto request)
        {
            var adminIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(adminIdClaim, out var currentAdminId))
            {
                return Unauthorized(new { success = false, message = "Không xác định được danh tính Admin." });
            }

            await _adminService.ToggleUserStatusAsync(id, request, currentAdminId);
            return Ok(new { success = true, message = "Cập nhật trạng thái người dùng thành công." });
        }

        [HttpGet("Users/{id}/transactions")]
        public async Task<IActionResult> GetUserTransactions(long id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _adminService.GetUserTransactionsAsync(id, page, pageSize);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("Users/{id}/overview")]
        public async Task<IActionResult> GetUserOverview(long id)
        {
            var result = await _adminService.GetUserOverviewAsync(id);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("Badges")]
        public async Task<IActionResult> GetBadges()
        {
            var result = await _adminService.GetBadgesAsync();
            return Ok(new { success = true, data = result });
        }
    }

    public class UpdateProductStatusRequest
    {
        public string Status { get; set; } = null!;
        public string? Note { get; set; }
    }
}
