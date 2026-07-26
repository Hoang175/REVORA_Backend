using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using REVORA_BE.DTOs.Request;
using REVORA_BE.Hubs;
using REVORA_BE.Models;
using REVORA_BE.Repositories.Interfaces;
using REVORA_BE.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepository;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly REVORA_BE.Models.AppDbContext _dbContext;
        private readonly IEmailService _emailService;

        public ChatService(IChatRepository chatRepository, IHubContext<ChatHub> hubContext, REVORA_BE.Models.AppDbContext dbContext, IEmailService emailService)
        {
            _chatRepository = chatRepository;
            _hubContext = hubContext;
            _dbContext = dbContext;
            _emailService = emailService;
        }

        public async Task<object> SendMessageAsync(long senderId, SendMessageRequestDto request)
        {
            long user1Id = Math.Min(senderId, request.ReceiverId);
            long user2Id = Math.Max(senderId, request.ReceiverId);

            var conversation = await _chatRepository.GetConversationAsync(user1Id, user2Id);
            if (conversation == null)
            {
                conversation = await _chatRepository.CreateConversationAsync(user1Id, user2Id);
            }

            var message = new Message
            {
                ConversationId = conversation.ConversationId,
                SenderId = senderId,
                Content = request.Content,
                AttachmentUrl = request.AttachmentUrl,
                ProductRefId = request.ProductRefId,
                CreatedAt = DateTime.UtcNow
            };

            await _chatRepository.AddMessageAsync(message);
            await _chatRepository.UpdateConversationLastMessageAtAsync(conversation.ConversationId, message.CreatedAt);

            // Re-fetch to get included ProductRef for payload
            var savedMessage = await _chatRepository.GetMessageByIdAsync(message.MessageId);

            var msgPayload = BuildMessagePayload(savedMessage!);

            // SignalR - Gửi cho tất cả các thiết bị của Receiver
            bool isReceiverOnline = false;
            if (ChatHub.UserConnections.TryGetValue(request.ReceiverId, out var receiverConnections))
            {
                lock (receiverConnections)
                {
                    if (receiverConnections.Count > 0)
                    {
                        isReceiverOnline = true;
                        foreach (var conn in receiverConnections)
                        {
                            _ = _hubContext.Clients.Client(conn).SendAsync("ReceiveMessage", msgPayload);
                        }
                    }
                }
            }

            // Gửi email nếu Receiver offline
            if (!isReceiverOnline)
            {
                var sender = await _dbContext.Users.FindAsync(senderId);
                var receiver = await _dbContext.Users.FindAsync(request.ReceiverId);
                if (receiver != null && !string.IsNullOrEmpty(receiver.Email))
                {
                    string productName = savedMessage?.ProductRef?.Title ?? "một sản phẩm";
                    string senderName = sender?.FullName ?? sender?.Username ?? "Một người dùng";
                    
                    string subject = $"[REVORA] Bạn có tin nhắn mới từ {senderName}";
                    string body = $@"
                        <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                            <h2 style='color: #2D5A3D;'>Tin nhắn mới trên REVORA</h2>
                            <p>Xin chào <strong>{receiver.FullName ?? receiver.Username}</strong>,</p>
                            <p><strong>{senderName}</strong> đã liên hệ với bạn để trao đổi về sản phẩm <strong>""{productName}""</strong>.</p>
                            <p style='background: #f9f9f9; padding: 10px; border-left: 4px solid #2D5A3D; font-style: italic;'>""{request.Content}""</p>
                            <br/>
                            <p>Vui lòng truy cập trang web <a href='https://revora.io.vn/messages' style='color: #2D5A3D; font-weight: bold;'>REVORA</a> để trả lời tin nhắn kịp thời nhé!</p>
                            <br/>
                            <p>Trân trọng,<br/>Đội ngũ REVORA</p>
                        </div>
                    ";
                    _ = _emailService.SendEmailAsync(receiver.Email, subject, body);
                }
            }

            // SignalR - Gửi cho tất cả các thiết bị khác của Sender (để đồng bộ)
            if (ChatHub.UserConnections.TryGetValue(senderId, out var senderConnections))
            {
                lock (senderConnections)
                {
                    foreach (var conn in senderConnections)
                    {
                        _ = _hubContext.Clients.Client(conn).SendAsync("ReceiveMessage", msgPayload);
                    }
                }
            }

            return msgPayload;
        }

        public async Task<object> GetConversationsAsync(long currentUserId)
        {
            var conversations = await _chatRepository.GetConversationsForUserAsync(currentUserId);

            // Get all user IDs of partners
            var partnerIds = conversations.Select(c => c.User1Id == currentUserId ? c.User2Id : c.User1Id).Distinct().ToList();

            // Fetch their active badges
            var userBadges = await _dbContext.Users
                .AsNoTracking()
                .Where(u => partnerIds.Contains(u.UserId) && u.BadgeId != null)
                .Select(u => new { u.UserId, u.BadgeId })
                .ToListAsync();

            var badgeIds = userBadges.Select(ub => ub.BadgeId!.Value).Distinct().ToList();
            var badges = await _dbContext.Badges
                .AsNoTracking()
                .Where(b => badgeIds.Contains(b.BadgeId))
                .ToDictionaryAsync(b => b.BadgeId, b => b.Name);

            var userBadgeMap = userBadges.ToDictionary(
                ub => ub.UserId,
                ub => badges.TryGetValue(ub.BadgeId!.Value, out var name) ? name : null
            );

            var result = conversations.Select(c => new
            {
                c.ConversationId,
                LastMessageAt = c.LastMessageAt?.AddHours(7),
                Partner = c.User1Id == currentUserId 
                    ? new { c.User2!.UserId, c.User2.FullName, c.User2.AvatarUrl, IsOnline = ChatHub.UserConnections.ContainsKey(c.User2.UserId), BadgeName = userBadgeMap.TryGetValue(c.User2.UserId, out var name1) ? name1 : null } 
                    : new { c.User1!.UserId, c.User1.FullName, c.User1.AvatarUrl, IsOnline = ChatHub.UserConnections.ContainsKey(c.User1.UserId), BadgeName = userBadgeMap.TryGetValue(c.User1.UserId, out var name2) ? name2 : null },
                LastMessage = c.Messages.OrderByDescending(m => m.CreatedAt).Select(m => new 
                { 
                    m.Content, 
                    CreatedAt = m.CreatedAt.AddHours(7), 
                    m.SenderId, 
                    m.AttachmentUrl, 
                    m.IsRead,
                    m.IsRevoked,
                    m.IsEdited
                }).FirstOrDefault(),
                UnreadCount = c.Messages.Count(m => m.SenderId != currentUserId && !m.IsRead)
            });

            return result;
        }

        public async Task<object> GetMessagesAsync(long currentUserId, long receiverId)
        {
            long user1Id = Math.Min(currentUserId, receiverId);
            long user2Id = Math.Max(currentUserId, receiverId);

            var messages = await _chatRepository.GetMessagesAsync(user1Id, user2Id, currentUserId);

            return messages.Select(BuildMessagePayload);
        }

        public async Task<bool> MarkAsReadAsync(long currentUserId, long partnerId)
        {
            var unreadMessages = await _chatRepository.GetUnreadMessagesFromPartnerAsync(currentUserId, partnerId);
            if (unreadMessages.Any())
            {
                await _chatRepository.MarkMessagesAsReadAsync(unreadMessages);

                // SignalR - Notify the partner that their messages were read
                if (ChatHub.UserConnections.TryGetValue(partnerId, out var receiverConnections))
                {
                    lock (receiverConnections)
                    {
                        foreach (var conn in receiverConnections)
                        {
                            _ = _hubContext.Clients.Client(conn).SendAsync("MessagesRead", currentUserId);
                        }
                    }
                }
            }
            return true;
        }

        public async Task<bool> MarkAsUnreadAsync(long currentUserId, long partnerId)
        {
            await _chatRepository.MarkMessagesAsUnreadAsync(currentUserId, partnerId);
            return true;
        }

        public async Task<bool> DeleteConversationAsync(long currentUserId, long partnerId)
        {
            await _chatRepository.HideConversationForUserAsync(currentUserId, partnerId);
            return true;
        }

        public async Task<object> EditMessageAsync(long currentUserId, long messageId, string newContent)
        {
            var message = await _chatRepository.GetMessageByIdAsync(messageId);
            if (message == null || message.SenderId != currentUserId || message.IsRevoked)
                return null!;

            if (message.CreatedAt.AddHours(1) < DateTime.UtcNow)
            {
                throw new Exception("Bạn chỉ có thể sửa tin nhắn trong vòng 1 giờ sau khi gửi.");
            }

            message.Content = newContent;
            message.IsEdited = true;
            await _chatRepository.UpdateMessageAsync(message);

            var payload = BuildMessagePayload(message);

            long partnerId = message.Conversation!.User1Id == currentUserId ? message.Conversation.User2Id : message.Conversation.User1Id;
            if (ChatHub.UserConnections.TryGetValue(partnerId, out var receiverConnections))
            {
                lock (receiverConnections)
                {
                    foreach (var conn in receiverConnections)
                    {
                        _ = _hubContext.Clients.Client(conn).SendAsync("MessageEdited", payload);
                    }
                }
            }
            if (ChatHub.UserConnections.TryGetValue(currentUserId, out var senderConnections))
            {
                lock (senderConnections)
                {
                    foreach (var conn in senderConnections)
                    {
                        _ = _hubContext.Clients.Client(conn).SendAsync("MessageEdited", payload);
                    }
                }
            }

            return payload;
        }

        public async Task<object> RevokeMessageAsync(long currentUserId, long messageId)
        {
            var message = await _chatRepository.GetMessageByIdAsync(messageId);
            if (message == null || message.SenderId != currentUserId)
                return null!;

            if (message.CreatedAt.AddHours(1) < DateTime.UtcNow)
            {
                throw new Exception("Bạn chỉ có thể thu hồi tin nhắn trong vòng 1 giờ sau khi gửi.");
            }

            message.IsRevoked = true;
            message.Content = null;
            message.AttachmentUrl = null;
            await _chatRepository.UpdateMessageAsync(message);

            var payload = BuildMessagePayload(message);

            long partnerId = message.Conversation!.User1Id == currentUserId ? message.Conversation.User2Id : message.Conversation.User1Id;
            if (ChatHub.UserConnections.TryGetValue(partnerId, out var receiverConnections))
            {
                lock (receiverConnections)
                {
                    foreach (var conn in receiverConnections)
                    {
                        _ = _hubContext.Clients.Client(conn).SendAsync("MessageRevoked", payload);
                    }
                }
            }
            if (ChatHub.UserConnections.TryGetValue(currentUserId, out var senderConnections))
            {
                lock (senderConnections)
                {
                    foreach (var conn in senderConnections)
                    {
                        _ = _hubContext.Clients.Client(conn).SendAsync("MessageRevoked", payload);
                    }
                }
            }

            return payload;
        }

        private object BuildMessagePayload(Message m)
        {
            long receiverId = m.Conversation!.User1Id == m.SenderId ? m.Conversation.User2Id : m.Conversation.User1Id;
            return new
            {
                id = m.MessageId,
                senderId = m.SenderId,
                receiverId = receiverId,
                text = m.IsRevoked ? "Tin nhắn đã bị thu hồi" : m.Content,
                imageUrl = m.IsRevoked ? null : m.AttachmentUrl,
                time = m.CreatedAt.AddHours(7).ToString("HH:mm"),
                read = m.IsRead,
                isRevoked = m.IsRevoked,
                isEdited = m.IsEdited,
                product = (!m.IsRevoked && m.ProductRef != null) ? new
                {
                    id = m.ProductRef.ProductId,
                    name = m.ProductRef.Title,
                    price = m.ProductRef.Price,
                    image = m.ProductRef.ProductImages.FirstOrDefault()?.ImageUrl
                } : null
            };
        }
    }
}
