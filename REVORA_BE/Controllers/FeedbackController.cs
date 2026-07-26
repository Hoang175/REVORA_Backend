using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.DTOs;
using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Services.Interfaces;
using System.Security.Claims;

namespace REVORA_BE.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class FeedbackController : ControllerBase
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackRequestDto dto, CancellationToken ct)
        {
            long? userId = null;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out var id))
                {
                    userId = id;
                }
            }

            var result = await _feedbackService.SubmitFeedbackAsync(userId, dto, ct);
            return Ok(ApiResponse<FeedbackResponseDto>.Ok(result, "Cảm ơn bạn đã đóng góp ý kiến!"));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllFeedbacks([FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var result = await _feedbackService.GetAllFeedbacksAsync(page, pageSize, ct);
            return Ok(ApiResponse<PagedResult<FeedbackResponseDto>>.Ok(result, "Lấy danh sách thành công."));
        }

        [HttpPut("{feedbackId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(long feedbackId, [FromBody] string status, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return BadRequest(new ApiResponse<object>(null, "Status không hợp lệ.") { Success = false });
            }

            await _feedbackService.UpdateFeedbackStatusAsync(feedbackId, status, ct);
            return Ok(ApiResponse<object>.Ok(null, "Cập nhật trạng thái thành công."));
        }
    }
}
