using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.Services.Implementations;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize] 
    public class MediaController : ControllerBase
    {
        private readonly CloudinaryService _cloudinaryService;

        public MediaController(CloudinaryService cloudinaryService)
        {
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("upload-images")]
        [Consumes("multipart/form-data")] 
        public async Task<IActionResult> UploadImages( List<IFormFile> files)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return Unauthorized(new { success = false, message = "Không thể xác thực người dùng. Vui lòng đăng nhập lại." });
                }

                if (files == null || files.Count == 0)
                    return BadRequest(new { success = false, message = "Vui lòng chọn ít nhất 1 file ảnh." });

                var uploadedUrls = new List<string>();

                string folderPath = $"REVORA_Media/Products/User_{userId}";

                foreach (var file in files)
                {
                    // Lọc 1: Bắt buộc là file ảnh
                    if (!file.ContentType.StartsWith("image/"))
                        return BadRequest(new { success = false, message = $"File {file.FileName} không hợp lệ. Chỉ chấp nhận định dạng hình ảnh." });

                    // Lọc 2: Giới hạn 5MB cho mỗi ảnh
                    if (file.Length > 5 * 1024 * 1024)
                        return BadRequest(new { success = false, message = $"Ảnh {file.FileName} vượt quá giới hạn 5MB." });

                    // Gọi Service để upload
                    var url = await _cloudinaryService.UploadImageAsync(file, folderPath);
                    if (!string.IsNullOrEmpty(url))
                    {
                        uploadedUrls.Add(url);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Tải ảnh thành công!",
                    urls = uploadedUrls
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("upload-video")]
        [Consumes("multipart/form-data")] 
        public async Task<IActionResult> UploadVideo( IFormFile file)
        {
            try
            {

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return Unauthorized(new { success = false, message = "Không thể xác thực người dùng. Vui lòng đăng nhập lại." });
                }

                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "Vui lòng chọn video." });

                // Validate 1: Chỉ cho phép video
                if (!file.ContentType.StartsWith("video/"))
                    return BadRequest(new { success = false, message = "Định dạng file không được hỗ trợ. Vui lòng chọn file Video." });

                // Validate 2: Giới hạn 30MB
                if (file.Length > 30 * 1024 * 1024)
                    return BadRequest(new { success = false, message = "Dung lượng video quá lớn. Vui lòng tải video dưới 30MB." });

                // Đường dẫn thư mục riêng biệt cho Video Shorts
                string folderPath = $"REVORA_Media/Shorts/User_{userId}";

                var url = await _cloudinaryService.UploadVideoAsync(file, folderPath);

                return Ok(new
                {
                    success = true,
                    message = "Tải video thành công!",
                    url = url
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost("upload-avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return Unauthorized(new { success = false, message = "Không thể xác thực người dùng. Vui lòng đăng nhập lại." });
                }

                if (file == null || file.Length == 0)
                    return BadRequest(new { success = false, message = "Vui lòng chọn ảnh đại diện." });

                // Bắt buộc là file ảnh
                if (!file.ContentType.StartsWith("image/"))
                    return BadRequest(new { success = false, message = "File không hợp lệ. Chỉ chấp nhận định dạng hình ảnh." });

                // Giới hạn 5MB
                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(new { success = false, message = "Ảnh vượt quá giới hạn 5MB." });

                string folderPath = $"REVORA_Media/Avatars/User_{userId}";
                var url = await _cloudinaryService.UploadImageAsync(file, folderPath);

                if (string.IsNullOrEmpty(url))
                {
                    return BadRequest(new { success = false, message = "Lỗi tải ảnh lên." });
                }

                return Ok(new
                {
                    success = true,
                    message = "Tải ảnh đại diện thành công!",
                    url = url
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}