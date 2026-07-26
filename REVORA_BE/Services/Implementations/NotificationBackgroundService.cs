using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Hubs;
using REVORA_BE.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Implementations
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(IServiceProvider serviceProvider, ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("NotificationBackgroundService is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessScheduledNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing scheduled notifications.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("NotificationBackgroundService is stopping.");
        }

        private async Task ProcessScheduledNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<ChatHub>>();

            var now = DateTime.UtcNow;

            var pendingNotifications = await context.Notifications
                .Where(n => !n.IsSent && n.ScheduledAt <= now)
                .ToListAsync();

            if (!pendingNotifications.Any()) return;

            foreach (var notification in pendingNotifications)
            {
                notification.IsSent = true;

                if (ChatHub.UserConnections.TryGetValue(notification.UserId, out var connections))
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
                    _ = hubContext.Clients.Clients(connections.ToList()).SendAsync("NewNotification", payload);
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation($"Processed and sent {pendingNotifications.Count} scheduled notifications.");
        }
    }
}
