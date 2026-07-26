using REVORA_BE.DTOs.Response;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationResponseDto>> GetUserNotificationsAsync(long userId);
        Task<bool> MarkAsReadAsync(Guid notificationId, long userId);
        Task<bool> MarkAllAsReadAsync(long userId);
        Task CreateNotificationAsync(long userId, string type, string title, string message, string? referenceId = null);
    }
}
