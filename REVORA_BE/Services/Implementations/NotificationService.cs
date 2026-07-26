using Microsoft.EntityFrameworkCore;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Models;
using REVORA_BE.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using REVORA_BE.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public NotificationService(AppDbContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<List<NotificationResponseDto>> GetUserNotificationsAsync(long userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && n.IsSent)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var result = new List<NotificationResponseDto>();
            var now = DateTime.UtcNow;

            foreach (var n in notifications)
            {
                string timeString = GetRelativeTime(n.CreatedAt, now);

                result.Add(new NotificationResponseDto
                {
                    Id = n.NotificationId,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    Time = timeString,
                    Read = n.IsRead,
                    ReferenceId = n.ReferenceId
                });
            }

            return result;
        }

        public async Task<bool> MarkAsReadAsync(Guid notificationId, long userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(long userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (!unreadNotifications.Any()) return true;

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task CreateNotificationAsync(long userId, string type, string title, string message, string? referenceId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                ReferenceId = referenceId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            if (ChatHub.UserConnections.TryGetValue(userId, out var connections))
            {
                var payload = new NotificationResponseDto
                {
                    Id = notification.NotificationId,
                    Type = notification.Type,
                    Title = notification.Title,
                    Message = notification.Message,
                    Time = "Vừa xong",
                    Read = false,
                    ReferenceId = notification.ReferenceId
                };
                await _hubContext.Clients.Clients(connections.ToList()).SendAsync("NewNotification", payload);
            }
        }

        private string GetRelativeTime(DateTime createdAt, DateTime now)
        {
            var diff = now - createdAt;
            if (diff.TotalMinutes < 1) return "Vừa xong";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
            if (diff.TotalDays < 30) return $"{(int)diff.TotalDays} ngày trước";
            if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)} tháng trước";
            return $"{(int)(diff.TotalDays / 365)} năm trước";
        }
    }
}
