using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.DTOs.Request;
using REVORA_BE.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/match-trade")]
    [ApiController]
    [Authorize]
    public class MatchTradeController : ControllerBase
    {
        private readonly IMatchTradeService _matchTradeService;

        public MatchTradeController(IMatchTradeService matchTradeService)
        {
            _matchTradeService = matchTradeService;
        }

        /// <summary>Thống kê cộng đồng — số người tham gia & sản phẩm chờ trao đổi.</summary>
        [HttpGet("stats")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _matchTradeService.GetCommunityStatsAsync();
            return Ok(new { success = true, message = "Lấy thống kê thành công", data = stats });
        }

        /// <summary>Bước 1: Sản phẩm của tôi đang bán (để chọn đem đi trao đổi).</summary>
        [HttpGet("my-products")]
        public async Task<IActionResult> GetMyProducts()
        {
            var data = await _matchTradeService.GetMyOfferingProductsAsync(GetUserId());
            return Ok(new { success = true, message = "Lấy danh sách sản phẩm thành công", data });
        }

        /// <summary>Bước 2: Khoảng giá & khu vực kèm số liệu.</summary>
        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var data = await _matchTradeService.GetFilterOptionsAsync(GetUserId());
            return Ok(new { success = true, message = "Lấy tùy chọn lọc thành công", data });
        }

        /// <summary>Xem trước số SP / người dự kiến trước khi bắt đầu.</summary>
        [HttpPost("preview")]
        public async Task<IActionResult> PreviewFilters([FromBody] PreviewMatchFiltersRequestDto request)
        {
            var data = await _matchTradeService.PreviewFiltersAsync(GetUserId(), request);
            return Ok(new { success = true, message = "Xem trước bộ lọc thành công", data });
        }

        /// <summary>Bước 3: Bắt đầu phiên Match.</summary>
        [HttpPost("sessions")]
        public async Task<IActionResult> StartSession([FromBody] StartMatchSessionRequestDto request)
        {
            try
            {
                var data = await _matchTradeService.StartSessionAsync(GetUserId(), request);
                return Ok(new { success = true, message = "Bắt đầu phiên Match thành công", data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("sessions/active")]
        public async Task<IActionResult> GetActiveSession()
        {
            var data = await _matchTradeService.GetActiveSessionAsync(GetUserId());
            if (data == null)
                return NotFound(new { success = false, message = "Không có phiên Match đang hoạt động." });
            return Ok(new { success = true, data });
        }

        /// <summary>Bước 4: Lấy sản phẩm tiếp theo để vuốt.</summary>
        [HttpGet("sessions/{sessionId}/next")]
        public async Task<IActionResult> GetNextCard(long sessionId)
        {
            try
            {
                var data = await _matchTradeService.GetNextCardAsync(GetUserId(), sessionId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Bước 4: Vuốt trái (pass) / phải (like).</summary>
        [HttpPost("sessions/{sessionId}/swipe")]
        public async Task<IActionResult> Swipe(long sessionId, [FromBody] MatchSwipeRequestDto request)
        {
            try
            {
                var data = await _matchTradeService.SwipeAsync(GetUserId(), sessionId, request);
                return Ok(new { success = true, message = data.Message, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Lấy danh sách sản phẩm của target user đang mang đi trao đổi.</summary>
        [HttpGet("sessions/user/{targetUserId}/offering-products")]
        public async Task<IActionResult> GetTargetOfferingProducts(long targetUserId)
        {
            try
            {
                var data = await _matchTradeService.GetTargetOfferingProductsAsync(targetUserId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Bước 4: Vuốt nhiều (bulk swipe) từ notification.</summary>
        [HttpPost("notifications/bulk-swipe")]
        public async Task<IActionResult> BulkSwipe([FromBody] MatchBulkSwipeRequestDto request)
        {
            try
            {
                var data = await _matchTradeService.BulkSwipeAsync(GetUserId(), request);
                return Ok(new { success = true, message = data.Message, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Thoát phiên theo ID.</summary>
        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> EndSession(long sessionId)
        {
            try
            {
                await _matchTradeService.EndSessionAsync(GetUserId(), sessionId);
                return Ok(new { success = true, message = "Đã kết thúc phiên Match." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Thoát phiên hiện tại đang Active (dùng cho Logout/Close tab).</summary>
        [HttpDelete("sessions/active")]
        public async Task<IActionResult> EndActiveSession()
        {
            try
            {
                await _matchTradeService.EndActiveSessionAsync(GetUserId());
                return Ok(new { success = true, message = "Đã kết thúc phiên Match hiện tại." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Check for active negotiation session (TradeMatch).</summary>
        [HttpGet("/api/v1/match/current-session")]
        [HttpGet("current-session")]
        public async Task<IActionResult> GetCurrentSession()
        {
            try
            {
                var data = await _matchTradeService.GetMyMatchesAsync(GetUserId(), "Active");
                var activeMatch = data.FirstOrDefault();
                
                if (activeMatch != null)
                {
                    return Ok(new { hasActiveSession = true, tradeMatchId = activeMatch.TradeMatchId });
                }
                
                return Ok(new { hasActiveSession = false });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Bước 5–6: Danh sách Match của tôi.</summary>
        [HttpGet("matches")]
        public async Task<IActionResult> GetMyMatches([FromQuery] string? status = null)
        {
            var data = await _matchTradeService.GetMyMatchesAsync(GetUserId(), status);
            return Ok(new { success = true, message = "Lấy danh sách Match thành công", data });
        }

        [HttpGet("matches/{tradeMatchId}")]
        public async Task<IActionResult> GetMatchDetail(long tradeMatchId)
        {
            var data = await _matchTradeService.GetMatchDetailAsync(GetUserId(), tradeMatchId);
            if (data == null)
                return NotFound(new { success = false, message = "Không tìm thấy Match." });
            return Ok(new { success = true, data });
        }

        /// <summary>Bước 7: Xác nhận danh sách sản phẩm đem ra thương lượng.</summary>
        [HttpPost("matches/{tradeMatchId}/negotiate")]
        public async Task<IActionResult> Negotiate(long tradeMatchId, [FromBody] MatchNegotiateRequestDto request)
        {
            try
            {
                var data = await _matchTradeService.NegotiateAsync(GetUserId(), tradeMatchId, request);
                return Ok(new { success = true, message = data.Message, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }



        /// <summary>Bước 8: Đồng ý trao đổi (hoàn tất giao dịch trong chat).</summary>
        [HttpPost("matches/{tradeMatchId}/confirm")]
        public async Task<IActionResult> ConfirmTrade(long tradeMatchId)
        {
            try
            {
                var data = await _matchTradeService.ConfirmTradeAsync(GetUserId(), tradeMatchId);
                return Ok(new { success = true, message = data.Message, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Từ chối xác nhận trao đổi.</summary>
        [HttpPost("matches/{tradeMatchId}/decline-confirm")]
        public async Task<IActionResult> DeclineConfirmTrade(long tradeMatchId)
        {
            try
            {
                var data = await _matchTradeService.DeclineConfirmAsync(GetUserId(), tradeMatchId);
                return Ok(new { success = true, message = data.Message, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Hủy phiên Match từ popup Match Thành Công.</summary>
        [HttpPost("matches/{tradeMatchId}/cancel")]
        public async Task<IActionResult> CancelMatch(long tradeMatchId, [FromQuery] bool isExpired = false)
        {
            try
            {
                var data = await _matchTradeService.CancelMatchAsync(GetUserId(), tradeMatchId, isExpired);
                return Ok(new { success = true, message = data.Message, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Rời khỏi trao đổi (Hủy kèo).</summary>
        [HttpPost("matches/{tradeMatchId}/leave")]
        public async Task<IActionResult> LeaveTrade(long tradeMatchId)
        {
            try
            {
                var data = await _matchTradeService.LeaveTradeAsync(GetUserId(), tradeMatchId);
                return Ok(new { success = true, message = data.Message, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Hoàn tất phiên giao dịch và xóa session của người dùng (Không thông báo cho đối phương).</summary>
        [HttpPost("matches/{tradeMatchId}/finish")]
        public async Task<IActionResult> FinishTrade(long tradeMatchId)
        {
            try
            {
                var data = await _matchTradeService.FinishTradeAsync(GetUserId(), tradeMatchId);
                return Ok(new { success = true, message = data.Message, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Danh sách sản phẩm tôi đã Tym trong phiên hiện tại.</summary>
        [HttpGet("sessions/{sessionId}/my-likes")]
        public async Task<IActionResult> GetMyLikes(long sessionId)
        {
            var data = await _matchTradeService.GetMyLikedProductsAsync(GetUserId(), sessionId);
            return Ok(new { success = true, data });
        }

        /// <summary>Bỏ thích sản phẩm trong phiên hiện tại.</summary>
        [HttpDelete("sessions/{sessionId}/likes/{productId}")]
        public async Task<IActionResult> UnlikeProduct(long sessionId, long productId)
        {
            try
            {
                await _matchTradeService.UnlikeProductAsync(GetUserId(), sessionId, productId);
                return Ok(new { success = true, message = "Đã xóa sản phẩm khỏi danh sách yêu thích." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>Hộp thư — ai đó đã Tym sản phẩm của tôi.</summary>
        [HttpGet("interest-inbox")]
        public async Task<IActionResult> GetInterestInbox()
        {
            var data = await _matchTradeService.GetInterestInboxAsync(GetUserId());
            return Ok(new { success = true, data });
        }

        /// <summary>Danh sách tất cả sản phẩm mà 2 bên đã Tym trong 1 Match.</summary>
        [HttpGet("matches/{tradeMatchId}/mutual-likes")]
        public async Task<IActionResult> GetMutualLikes(long tradeMatchId)
        {
            try
            {
                var data = await _matchTradeService.GetMutualLikesInTradeAsync(GetUserId(), tradeMatchId);
                return Ok(new { success = true, data });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        private long GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(claim, out var userId))
                throw new UnauthorizedAccessException("Không thể xác thực người dùng.");
            return userId;
        }
    }
}
