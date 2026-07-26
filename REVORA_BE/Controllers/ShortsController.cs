using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.DTOs.Request;
using REVORA_BE.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ShortsController : ControllerBase
    {
        private readonly IShortService _shortService;

        public ShortsController(IShortService shortService)
        {
            _shortService = shortService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeedShorts()
        {
            long? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long parsedId)) currentUserId = parsedId;

            var shorts = await _shortService.GetFeedShortsAsync(currentUserId);
            return Ok(new { success = true, data = shorts });
        }

        [HttpPost("{id}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleLike(long id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            var isLiked = await _shortService.ToggleLikeAsync(userId, id);
            return Ok(new { success = true, isLiked = isLiked });
        }

        [HttpGet("{id}/comments")]
        [AllowAnonymous]
        public async Task<IActionResult> GetComments(long id)
        {
            long? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long parsedId)) currentUserId = parsedId;

            var comments = await _shortService.GetShortCommentsAsync(id, currentUserId);
            return Ok(new { success = true, data = comments });
        }

        [HttpPost("{id}/comments")]
        [Authorize]
        public async Task<IActionResult> AddComment(long id, [FromBody] CreateCommentRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { success = false, message = "Nội dung bình luận không được rỗng." });

            var comment = await _shortService.AddCommentAsync(userId, id, request);
            return Ok(new { success = true, data = comment });
        }

        [HttpPut("{id}/comments/{commentId}")]
        [Authorize]
        public async Task<IActionResult> UpdateComment(long id, long commentId, [FromBody] UpdateCommentRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { success = false, message = "Nội dung bình luận không được rỗng." });

            var comment = await _shortService.UpdateCommentAsync(userId, commentId, request);
            if (comment == null)
                return NotFound(new { success = false, message = "Bình luận không tồn tại hoặc bạn không có quyền sửa." });

            return Ok(new { success = true, data = comment });
        }

        [HttpDelete("{id}/comments/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(long id, long commentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            var success = await _shortService.DeleteCommentAsync(userId, commentId);
            if (!success)
                return NotFound(new { success = false, message = "Bình luận không tồn tại hoặc bạn không có quyền xóa." });

            return Ok(new { success = true });
        }

        [HttpPost("{id}/comments/{commentId}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleLikeComment(long id, long commentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            var isLiked = await _shortService.ToggleLikeCommentAsync(userId, commentId);
            return Ok(new { success = true, isLiked = isLiked });
        }

        public class ChangeShortStatusRequest
        {
            public string Status { get; set; } = null!;
        }

        [HttpPut("{id}/status")]
        [Authorize]
        public async Task<IActionResult> ChangeShortStatus(long id, [FromBody] ChangeShortStatusRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            var success = await _shortService.ChangeShortStatusAsync(userId, id, request.Status);
            if (!success)
                return NotFound(new { success = false, message = "Short video không tồn tại hoặc lỗi xử lý." });

            return Ok(new { success = true, message = "Cập nhật trạng thái Short Video thành công." });
        }
    }
}