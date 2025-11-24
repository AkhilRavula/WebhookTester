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
                        var context = scope.ServiceProvider.GetRequiredService<WebhookContext>();
                        
                        // Delete webhooks created more than 30 days ago
                        var cutoffDate = DateTime.UtcNow.AddDays(-30);
                        
                        var oldWebhooks = await context.WebhookEndpoints
                            .Where(w => w.CreatedAt < cutoffDate)
                            .ToListAsync(stoppingToken);

                        if (oldWebhooks.Any())
                        {
                            context.WebhookEndpoints.RemoveRange(oldWebhooks);
                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Deleted {count} old webhooks.", oldWebhooks.Count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during cleanup.");
                }

                // Run once a day
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
