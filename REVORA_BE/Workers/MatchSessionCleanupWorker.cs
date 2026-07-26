using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using REVORA_BE.Services.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace REVORA_BE.Workers
{
    public class MatchSessionCleanupWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MatchSessionCleanupWorker> _logger;

        public MatchSessionCleanupWorker(IServiceProvider serviceProvider, ILogger<MatchSessionCleanupWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MatchSessionCleanupWorker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredSessionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing MatchSessionCleanupWorker.");
                }

                // Chạy mỗi 1 phút
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("MatchSessionCleanupWorker is stopping.");
        }

        private async Task CleanupExpiredSessionsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var matchTradeService = scope.ServiceProvider.GetRequiredService<IMatchTradeService>();

            var expiryThreshold = DateTime.UtcNow.AddHours(-1);

            var expiredSessionIds = await context.MatchSessions
                .Where(s => s.Status == MatchSessionStatus.Active.ToString() && s.StartedAt <= expiryThreshold)
                .Select(s => s.MatchSessionId)
                .ToListAsync(stoppingToken);

            if (expiredSessionIds.Any())
            {
                _logger.LogInformation($"Found {expiredSessionIds.Count} expired Match Sessions. Cleaning up...");

                foreach (var sessionId in expiredSessionIds)
                {
                    try
                    {
                        await matchTradeService.ExpireSessionAsync(sessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to expire session {sessionId}");
                    }
                }
            }
        }
    }
}
