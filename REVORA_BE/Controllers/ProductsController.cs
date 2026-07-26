using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.DTOs.Request;
using REVORA_BE.Exceptions;
using REVORA_BE.Models;
using REVORA_BE.Services;
using REVORA_BE.Services.Interfaces;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequestDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!long.TryParse(userIdClaim, out long userId))
                {
                    return Unauthorized(new { success = false, message = "Không thể xác thực người dùng. Vui lòng đăng nhập lại." });
                }


                long productId = await _productService.CreateProductAsync(userId, request);

                return Ok(new
                {
                    success = true,
                    message = "Đăng sản phẩm thành công!",
                    productId = productId
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

        [HttpGet("featured")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 10)
        {
            var products = await _productService.GetHomeProductsAsync("featured", limit);
            return Ok(new { success = true, data = products });
        }

        [HttpGet("loved")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMostLovedProducts([FromQuery] int limit = 10)
        {
            var products = await _productService.GetHomeProductsAsync("loved", limit);
            return Ok(new { success = true, data = products });
        }

        [HttpGet("most-viewed")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMostViewedProducts([FromQuery] int limit = 10)
        {
            var products = await _productService.GetHomeProductsAsync("most-viewed", limit);
            return Ok(new { success = true, data = products });
        }

        [HttpGet("newest")]
        [AllowAnonymous]
        public async Task<IActionResult> GetNewestProducts([FromQuery] int limit = 10)
        {
            var products = await _productService.GetHomeProductsAsync("newest", limit);
            return Ok(new { success = true, data = products });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProducts([FromQuery] ProductFilterRequestDto filter)
        {
            var result = await _productService.GetProductsWithFilterAsync(filter);
            return Ok(new { success = true, data = result });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProducts([FromQuery] string status = "all", [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100, System.Threading.CancellationToken ct = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("User identification invalid.", "InvalidUserClaim");
            }

            var result = await _productService.GetProductsBySellerAsync(userId, status, pageNumber, pageSize, ct, isManageMode: true);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("me/deleted")]
        [Authorize]
        public async Task<IActionResult> GetMyDeletedProducts([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
            {
                throw new UnauthorizedException("User identification invalid.", "InvalidUserClaim");
            }

            var result = await _productService.GetDeletedProductsBySellerAsync(userId, pageNumber, pageSize, ct);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("seller/{sellerId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductsBySeller(long sellerId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100, System.Threading.CancellationToken ct = default)
        {
            var result = await _productService.GetProductsBySellerAsync(sellerId, "all", pageNumber, pageSize, ct);
            return Ok(new { success = true, data = result });
        }
        // thanh end

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductDetail(long id)
        {
            try
            {
                long? currentUserId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (long.TryParse(userIdClaim, out long parsedId)) currentUserId = parsedId;

                var product = await _productService.GetProductDetailAsync(id, currentUserId);
                if (product == null)
                    return NotFound(new { success = false, message = "Không tìm thấy sản phẩm." });

                return Ok(new { success = true, data = product });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProduct(long id, [FromBody] UpdateProductRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            var success = await _productService.UpdateProductAsync(id, userId, request);
            if (!success)
                return BadRequest(new { success = false, message = "Cập nhật thất bại hoặc bạn không có quyền sửa sản phẩm này." });

            return Ok(new { success = true, message = "Cập nhật sản phẩm thành công." });
        }

        [HttpPost("{id}/renew")]
        [Authorize]
        public async Task<IActionResult> RenewProduct(long id, [FromBody] RenewProductRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            try
            {
                var success = await _productService.RenewProductAsync(id, userId, request);
                if (!success)
                    return BadRequest(new { success = false, message = "Gia hạn thất bại hoặc bạn không có quyền sửa sản phẩm này." });

                return Ok(new { success = true, message = "Gia hạn thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPatch("{id}/status")]
        [Authorize]
        public async Task<IActionResult> ChangeProductStatus(long id, [FromBody] ChangeProductStatusRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            try
            {
                var success = await _productService.ChangeProductStatusAsync(id, userId, request.Status);
                if (!success)
                    return BadRequest(new { success = false, message = "Thay đổi trạng thái thất bại hoặc bạn không có quyền." });

                return Ok(new { success = true, message = "Cập nhật trạng thái thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public class SubmitAppealRequestDto
        {
            public string Reason { get; set; } = null!;
        }

        [HttpPost("{id}/appeal")]
        [Authorize]
        public async Task<IActionResult> SubmitAppeal(long id, [FromBody] SubmitAppealRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { success = false, message = "Vui lòng nhập lý do kháng cáo." });

            try
            {
                var success = await _productService.SubmitAppealAsync(id, userId, request.Reason);
                if (!success)
                    return BadRequest(new { success = false, message = "Kháng cáo thất bại." });

                return Ok(new { success = true, message = "Gửi yêu cầu kháng cáo thành công. Vui lòng chờ admin phản hồi." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteProduct(long id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            var success = await _productService.DeleteProductAsync(id, userId);
            if (!success)
                return BadRequest(new { success = false, message = "Xóa thất bại hoặc bạn không có quyền xóa sản phẩm này." });

            return Ok(new { success = true, message = "Sản phẩm đã được xóa." });
        }

        [HttpGet("my-credits")]
        [Authorize]
        public async Task<IActionResult> GetMyCredits()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Lỗi xác thực." });

            var postingBatches = await _productService.GetActiveCreditBatchesAsync(userId, 1);
            var featuredBatches = await _productService.GetActiveCreditBatchesAsync(userId, 2);

            int totalPosting = postingBatches.Sum(x => x.RemainingCredits);
            int totalFeatured = featuredBatches.Sum(x => x.RemainingCredits);

            return Ok(new
            {
                success = true,
                data = new
                {
                    postingCredits = totalPosting,
                    featuredCredits = totalFeatured
                }
            });
        }


        [HttpGet("{id}/comments")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductComments(long id)
        {
            long? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long parsedId)) currentUserId = parsedId;

            var comments = await _productService.GetProductCommentsAsync(id, currentUserId);
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
                return BadRequest(new { success = false, message = "Nội dung bình luận không được trống." });

            var newComment = await _productService.AddCommentAsync(userId, id, request);
            return Ok(new { success = true, data = newComment });
        }

        [HttpPost("{id}/comments/{commentId}/like")]
        [Authorize]
        public async Task<IActionResult> ToggleLikeComment(long id, long commentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            var isLiked = await _productService.ToggleLikeCommentAsync(userId, commentId);
            return Ok(new { success = true, isLiked = isLiked });
        }

        [HttpPut("{id}/comments/{commentId}")]
        [Authorize]
        public async Task<IActionResult> EditComment(long id, long commentId, [FromBody] UpdateCommentRequestDto request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            if (string.IsNullOrWhiteSpace(request.Content))
                return BadRequest(new { success = false, message = "Nội dung bình luận không được trống." });

            var updatedComment = await _productService.EditCommentAsync(userId, commentId, request);
            if (updatedComment == null)
                return NotFound(new { success = false, message = "Không tìm thấy bình luận." });

            return Ok(new { success = true, data = updatedComment });
        }

        [HttpDelete("{id}/comments/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(long id, long commentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!long.TryParse(userIdClaim, out long userId))
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập." });

            var success = await _productService.DeleteCommentAsync(userId, commentId);
            if (!success)
                return NotFound(new { success = false, message = "Không tìm thấy bình luận hoặc bạn không có quyền xóa." });

            return Ok(new { success = true, message = "Xóa bình luận thành công." });
        }



    }
}