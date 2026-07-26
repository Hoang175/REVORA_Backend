using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.Services.Interfaces;
using System.Security.Claims;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class WishlistsController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistsController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        [HttpPost("toggle/{productId}")]
        public async Task<IActionResult> ToggleWishlist(long productId, CancellationToken ct)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return Unauthorized(new { success = false, message = "Không thể xác thực người dùng. Vui lòng đăng nhập lại." });
                }

                var isAdded = await _wishlistService.ToggleWishlistAsync(userId, productId, ct);
                var message = isAdded ? "Đã thêm vào danh sách yêu thích" : "Đã xóa khỏi danh sách yêu thích";
                return Ok(new { success = true, message = message, isWishlisted = isAdded });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyWishlistProducts(CancellationToken ct)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return Unauthorized(new { success = false, message = "Không thể xác thực người dùng. Vui lòng đăng nhập lại." });
                }

                var products = await _wishlistService.GetMyWishlistProductsAsync(userId, ct);
                return Ok(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpGet("my-ids")]
        public async Task<IActionResult> GetMyWishlistProductIds(CancellationToken ct)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return Unauthorized(new { success = false, message = "Không thể xác thực người dùng. Vui lòng đăng nhập lại." });
                }

                var ids = await _wishlistService.GetMyWishlistProductIdsAsync(userId, ct);
                return Ok(new { success = true, data = ids });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
