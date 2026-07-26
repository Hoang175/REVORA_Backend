using Microsoft.EntityFrameworkCore;
using REVORA_BE.Data;
using REVORA_BE.Models;
using REVORA_BE.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Implementations
{
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetConversationAsync(long user1Id, long user2Id)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);
        }

        public async Task<Conversation> CreateConversationAsync(long user1Id, long user2Id)
        {
            var conversation = new Conversation { User1Id = user1Id, User2Id = user2Id };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task<Message> AddMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task UpdateConversationLastMessageAtAsync(long conversationId, DateTime time)
        {
            var conv = await _context.Conversations.FindAsync(conversationId);
            if (conv != null)
            {
                conv.LastMessageAt = time;
                // If a new message arrives, we should unhide it for both users
                conv.DeletedAtUser1 = null;
                conv.DeletedAtUser2 = null;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Conversation>> GetConversationsForUserAsync(long userId)
        {
            var query = _context.Conversations
                .Include(c => c.User1)
                .Include(c => c.User2)
                .Include(c => c.Messages)
                .Where(c => c.User1Id == userId || c.User2Id == userId);

            var list = await query.ToListAsync();

            // Filter out conversations hidden by the user
            return list.Where(c => 
            {
                var deletedAt = c.User1Id == userId ? c.DeletedAtUser1 : c.DeletedAtUser2;
                if (!deletedAt.HasValue) return true;
                // Only show if there's a new message AFTER the deleted time
                return c.Messages.Any(m => m.CreatedAt > deletedAt.Value);
            })
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToList();
        }

        public async Task<List<Message>> GetMessagesAsync(long user1Id, long user2Id, long currentUserId)
        {
            var conv = await _context.Conversations
                .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);
                
            if (conv == null) return new List<Message>();

            var deletedAt = conv.User1Id == currentUserId ? conv.DeletedAtUser1 : conv.DeletedAtUser2;

            var messages = await _context.Messages
                .Include(m => m.ProductRef)
                .ThenInclude(p => p.ProductImages)
                .Where(m => m.ConversationId == conv.ConversationId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            if (deletedAt.HasValue)
            {
                messages = messages.Where(m => m.CreatedAt > deletedAt.Value).ToList();
            }

            return messages;
        }

        public async Task<List<Message>> GetUnreadMessagesFromPartnerAsync(long currentUserId, long partnerId)
        {
            long user1Id = Math.Min(currentUserId, partnerId);
            long user2Id = Math.Max(currentUserId, partnerId);

            return await _context.Messages
                .Include(m => m.Conversation)
                .Where(m => m.Conversation!.User1Id == user1Id && m.Conversation.User2Id == user2Id 
                            && m.SenderId == partnerId 
                            && !m.IsRead)
                .ToListAsync();
        }

        public async Task MarkMessagesAsReadAsync(List<Message> messages)
        {
            foreach (var msg in messages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }

        public async Task MarkMessagesAsUnreadAsync(long currentUserId, long partnerId)
        {
            long user1Id = Math.Min(currentUserId, partnerId);
            long user2Id = Math.Max(currentUserId, partnerId);

            // Find the latest message received from partner
            var latestMessage = await _context.Messages
                .Include(m => m.Conversation)
                .Where(m => m.Conversation!.User1Id == user1Id && m.Conversation.User2Id == user2Id 
                            && m.SenderId == partnerId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            if (latestMessage != null && latestMessage.IsRead)
            {
                latestMessage.IsRead = false;
                latestMessage.ReadAt = null;
                await _context.SaveChangesAsync();
            }
        }

        public async Task HideConversationForUserAsync(long currentUserId, long partnerId)
        {
            long user1Id = Math.Min(currentUserId, partnerId);
            long user2Id = Math.Max(currentUserId, partnerId);

            var conv = await _context.Conversations
                .FirstOrDefaultAsync(c => c.User1Id == user1Id && c.User2Id == user2Id);

            if (conv != null)
            {
                if (conv.User1Id == currentUserId)
                    conv.DeletedAtUser1 = DateTime.UtcNow;
                else
                    conv.DeletedAtUser2 = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }

        public async Task<Message?> GetMessageByIdAsync(long messageId)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .Include(m => m.ProductRef)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        public async Task UpdateMessageAsync(Message message)
        {
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
        }
    }
}
