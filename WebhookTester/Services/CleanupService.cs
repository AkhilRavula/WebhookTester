using Microsoft.EntityFrameworkCore;
using WebhookTester.Data;

namespace WebhookTester.Services
{
    public class CleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CleanupService> _logger;

        public CleanupService(IServiceProvider serviceProvider, ILogger<CleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("CleanupService running at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var s3Storage = scope.ServiceProvider.GetRequiredService<S3StorageService>();
                        
                        // Delete webhook REQUESTS older than 30 days
                        // NOTE: Endpoints are NOT deleted - only requests are cleaned up
                        var cutoffDate = DateTime.UtcNow.AddDays(-30);
                        
                        var deletedCount = await s3Storage.DeleteOldRequestsAsync(cutoffDate);
                        
                        if (deletedCount > 0)
                        {
                            _logger.LogInformation("Deleted {count} old webhook requests.", deletedCount);
                        }
                        
                        // SQLite code (COMMENTED OUT - using S3 instead):
                        // var context = scope.ServiceProvider.GetRequiredService<WebhookContext>();
                        // var oldWebhooks = await context.WebhookEndpoints
                        //     .Where(w => w.CreatedAt < cutoffDate)
                        //     .ToListAsync(stoppingToken);
                        // if (oldWebhooks.Any())
                        // {
                        //     context.WebhookEndpoints.RemoveRange(oldWebhooks);
                        //     await context.SaveChangesAsync(stoppingToken);
                        //     _logger.LogInformation("Deleted {count} old webhooks.", oldWebhooks.Count);
                        // }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during cleanup.");
                }

                // Run once a month
                await Task.Delay(TimeSpan.FromDays(30), stoppingToken);
            }
        }
    }
}
