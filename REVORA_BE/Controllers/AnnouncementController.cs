using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.DTOs;
using REVORA_BE.Services;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;

        public AnnouncementController(IAnnouncementService announcementService)
        {
            _announcementService = announcementService;
        }

        [HttpGet("active")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveAnnouncements(CancellationToken ct)
        {
            var announcements = await _announcementService.GetActiveAnnouncementsAsync(ct);
            return Ok(ApiResponse<List<AnnouncementResponseDto>>.Ok(announcements));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAnnouncements(CancellationToken ct)
        {
            var announcements = await _announcementService.GetAllAnnouncementsAsync(ct);
            return Ok(ApiResponse<List<AnnouncementResponseDto>>.Ok(announcements));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAnnouncement([FromBody] REVORA_BE.DTOs.Request.AnnouncementCreateDto request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var result = await _announcementService.CreateAnnouncementAsync(request, ct);
            return Ok(ApiResponse<AnnouncementResponseDto>.Ok(result, "Tạo thành công."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAnnouncement(long id, [FromBody] REVORA_BE.DTOs.Request.AnnouncementUpdateDto request, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var result = await _announcementService.UpdateAnnouncementAsync(id, request, ct);
            if (result == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông báo." });
            }

            return Ok(ApiResponse<AnnouncementResponseDto>.Ok(result, "Cập nhật thành công."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAnnouncement(long id, CancellationToken ct)
        {
            var success = await _announcementService.DeleteAnnouncementAsync(id, ct);
            if (!success)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông báo." });
            }

            return Ok(new { success = true, message = "Xóa thành công." });
        }
    }
}
