using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [AllowAnonymous] // Đổi thành [Authorize] khi đã tích hợp JWT Login
    public class CreditPackagesController : ControllerBase
    {
        private readonly IPaidCreditPackageService _packageService;
        private readonly IUserCreditBatchService _userCreditBatchService;

        public CreditPackagesController(IPaidCreditPackageService packageService, IUserCreditBatchService userCreditBatchService)
        {
            _packageService = packageService;
            _userCreditBatchService = userCreditBatchService;
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetAllActivePackages()
        {
            try
            {
                var packages = await _packageService.GetAllActivePackagesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách các gói tín dụng thành công",
                    data = packages
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPackageById(long id)
        {
            try
            {
                var package = await _packageService.GetPackageByIdAsync(id);
                
                if (package == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy thông tin gói này!"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy chi tiết gói thành công",
                    data = package
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePackage(long id, [FromBody] REVORA_BE.DTOs.Request.AdminUpdatePackageRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var success = await _packageService.UpdatePackageAsync(id, request);
                if (!success)
                    return NotFound(new { success = false, message = "Không tìm thấy gói." });

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật gói thành công."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // BỔ SUNG: API 1 - LẤY CREDIT "POSTING" (ID = 1)
        [Authorize]
        [HttpGet("my-posting-credits")]
        public async Task<IActionResult> GetMyPostingCredits()
        {
            try
            {
                long userId = GetCurrentUserId();
                long postingCreditTypeId = 1;

                var wallet = await _userCreditBatchService.GetMyCreditsByTypeAsync(userId, postingCreditTypeId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin credit Đăng Tin thành công",
                    data = wallet
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // BỔ SUNG: API 2 - LẤY CREDIT "FEATURED" (ID = 2)
        [Authorize]
        [HttpGet("my-featured-credits")]
        public async Task<IActionResult> GetMyFeaturedCredits()
        {
            try
            {
                long userId = GetCurrentUserId();
                long featuredCreditTypeId = 2;

                var wallet = await _userCreditBatchService.GetMyCreditsByTypeAsync(userId, featuredCreditTypeId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin credit Nổi Bật thành công",
                    data = wallet
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("my-usage-history")]
        public async Task<IActionResult> GetMyUsageHistory()
        {
            try
            {
                long userId = GetCurrentUserId();

                var history = await _userCreditBatchService.GetMyUsageHistoryAsync(userId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch sử sử dụng credit thành công",
                    data = history
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // Xử lý chung phần lấy UserId
        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
            {
                if (Request.Headers.TryGetValue("X-Test-User-Id", out var testUserIdStr) && long.TryParse(testUserIdStr, out long testUserId))
                {
                    userId = testUserId;
                }
                else
                {
                    userId = 0; // Mặc định userId = 0 nếu không có session
                }
            }
            return userId;
        }
    }
}