using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using REVORA_BE.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace REVORA_BE.Workers
{
    public class TrashCleanupWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TrashCleanupWorker> _logger;

        public TrashCleanupWorker(IServiceProvider serviceProvider, ILogger<TrashCleanupWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TrashCleanupWorker is starting.");
            
            // Đợi 30 giây để đảm bảo migrations đã chạy xong trước khi query DB
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupTrashAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing TrashCleanupWorker.");
                }

                // Chạy mỗi 24 giờ
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }

            _logger.LogInformation("TrashCleanupWorker is stopping.");
        }

        private async Task CleanupTrashAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var thresholdDate = DateTime.UtcNow.AddDays(-30);

            var expiredProducts = await context.Products
                .Where(p => (p.ProductStatus == "Deleted" || p.ProductStatus == "AdminDeleted") && p.DeletedAt != null && p.DeletedAt <= thresholdDate)
                .ToListAsync(stoppingToken);

            if (expiredProducts.Any())
            {
                _logger.LogInformation($"Found {expiredProducts.Count} products in trash older than 30 days. Hard deleting...");

                context.Products.RemoveRange(expiredProducts);
                await context.SaveChangesAsync(stoppingToken);

                _logger.LogInformation($"Successfully hard deleted {expiredProducts.Count} products.");
            }
        }
    }
}
