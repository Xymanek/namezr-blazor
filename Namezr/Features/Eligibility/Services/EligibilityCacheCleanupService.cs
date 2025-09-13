using AutoRegisterInject;

namespace Namezr.Features.Eligibility.Services;

[RegisterSingleton]
internal class EligibilityCacheCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EligibilityCacheCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public EligibilityCacheCleanupService(
        IServiceProvider serviceProvider,
        ILogger<EligibilityCacheCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var cache = scope.ServiceProvider.GetRequiredService<IEligibilityCache>();
                
                await cache.CleanupExpiredEntriesAsync();
                _logger.LogDebug("Eligibility cache cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during eligibility cache cleanup");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
}