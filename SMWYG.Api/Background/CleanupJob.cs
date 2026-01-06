using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SMWYG.Api.Background
{
    public class CleanupJob : BackgroundService
    {
        private readonly ILogger<CleanupJob> _logger;
        private readonly IServiceProvider _services;
        private readonly TimeSpan _interval = TimeSpan.FromHours(24);

        public CleanupJob(ILogger<CleanupJob> logger, IServiceProvider services)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var cutoff = DateTime.UtcNow.AddDays(-30);
                    var oldMessages = db.Messages.Where(m => m.SentAt < cutoff).ToList();
                    if (oldMessages.Count > 0)
                    {
                        db.Messages.RemoveRange(oldMessages);
                        await db.SaveChangesAsync();
                        _logger.LogInformation("Deleted {Count} old messages.", oldMessages.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cleanup job failed.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
