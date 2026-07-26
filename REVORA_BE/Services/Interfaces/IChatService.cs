using REVORA_BE.DTOs.Request;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IChatService
    {
        Task<object> SendMessageAsync(long senderId, SendMessageRequestDto request);
        Task<object> GetConversationsAsync(long currentUserId);
        Task<object> GetMessagesAsync(long currentUserId, long receiverId);
        Task<bool> MarkAsReadAsync(long currentUserId, long partnerId);
        Task<bool> MarkAsUnreadAsync(long currentUserId, long partnerId);
        Task<bool> DeleteConversationAsync(long currentUserId, long partnerId);
        Task<object> EditMessageAsync(long currentUserId, long messageId, string newContent);
        Task<object> RevokeMessageAsync(long currentUserId, long messageId);
    }
}
