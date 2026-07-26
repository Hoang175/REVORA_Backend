using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.DTOs.Request;
using REVORA_BE.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace REVORA_BE.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        private long GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out long currentUserId))
                return currentUserId;
            return 0;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDto request)
        {
            long currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var result = await _chatService.SendMessageAsync(currentUserId, request);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            long currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var result = await _chatService.GetConversationsAsync(currentUserId);
            return Ok(new { success = true, data = result });
        }

        [HttpGet("{receiverId}/messages")]
        public async Task<IActionResult> GetMessages(long receiverId)
        {
            long currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var result = await _chatService.GetMessagesAsync(currentUserId, receiverId);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("{partnerId}/read")]
        public async Task<IActionResult> MarkAsRead(long partnerId)
        {
            long currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            await _chatService.MarkAsReadAsync(currentUserId, partnerId);
            return Ok(new { success = true });
        }

        [HttpPost("{partnerId}/unread")]
        public async Task<IActionResult> MarkAsUnread(long partnerId)
        {
            long currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            await _chatService.MarkAsUnreadAsync(currentUserId, partnerId);
            return Ok(new { success = true });
        }

        [HttpDelete("{partnerId}")]
        public async Task<IActionResult> DeleteConversation(long partnerId)
        {
            long currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            await _chatService.DeleteConversationAsync(currentUserId, partnerId);
            return Ok(new { success = true });
        }

        public class EditMessageRequestDto
        {
            public string Content { get; set; } = null!;
        }

        [HttpPut("message/{messageId}")]
        public async Task<IActionResult> EditMessage(long messageId, [FromBody] EditMessageRequestDto request)
        {
            long currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            try 
            {
                var result = await _chatService.EditMessageAsync(currentUserId, messageId, request.Content);
                if (result == null) return BadRequest(new { success = false, message = "Không thể sửa tin nhắn" });
                return Ok(new { success = true, data = result });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("message/{messageId}/revoke")]
        public async Task<IActionResult> RevokeMessage(long messageId)
        {
            long currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            try 
            {
                var result = await _chatService.RevokeMessageAsync(currentUserId, messageId);
                if (result == null) return BadRequest(new { success = false, message = "Không thể thu hồi tin nhắn" });
                return Ok(new { success = true, data = result });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}