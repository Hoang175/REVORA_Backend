using REVORA_BE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Interfaces
{
    public interface IChatRepository
    {
        Task<Conversation?> GetConversationAsync(long user1Id, long user2Id);
        Task<Conversation> CreateConversationAsync(long user1Id, long user2Id);
        Task<Message> AddMessageAsync(Message message);
        Task UpdateConversationLastMessageAtAsync(long conversationId, System.DateTime time);
        Task<List<Conversation>> GetConversationsForUserAsync(long userId);
        Task<List<Message>> GetMessagesAsync(long user1Id, long user2Id, long currentUserId);
        Task<List<Message>> GetUnreadMessagesFromPartnerAsync(long currentUserId, long partnerId);
        Task MarkMessagesAsReadAsync(List<Message> messages);
        Task MarkMessagesAsUnreadAsync(long currentUserId, long partnerId);
        Task HideConversationForUserAsync(long currentUserId, long partnerId);
        Task<Message?> GetMessageByIdAsync(long messageId);
        Task UpdateMessageAsync(Message message);
    }
}
