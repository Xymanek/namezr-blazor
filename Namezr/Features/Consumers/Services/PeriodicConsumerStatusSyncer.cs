using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Creators.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Consumers.Services;

[AutoConstructor]
public partial class PeriodicConsumerStatusSyncer : BackgroundService
{
    private readonly IEnumerable<IConsumerStatusManager> _consumerStatusManagers;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<PeriodicConsumerStatusSyncer> _logger;

    private static readonly TimeSpan SyncInterval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Periodic consumer status sync starting");

            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                await DoSync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Periodic consumer status sync failed");
            }

            stopwatch.Stop();
            TimeSpan sleepTime = SyncInterval - stopwatch.Elapsed;

            if (sleepTime > TimeSpan.Zero)
            {
                _logger.LogInformation("Periodic consumer status sync sleeping for {SleepTime}", sleepTime);
                await Task.Delay(sleepTime, stoppingToken);
            }
            else
            {
                _logger.LogWarning(
                    "Background sync took {executionTime} - longer than sync interval. Next sync starting instantly",
                    stopwatch.Elapsed
                );
            }
        }
    }

    private async Task DoSync(CancellationToken ct)
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        using var _ = Diagnostics.ActivitySource.StartActivity("PeriodicConsumerStatusSync");

        Dictionary<SupportServiceType, IConsumerStatusManager> managersPerService
            = _consumerStatusManagers.ToDictionary(x => x.ServiceType);

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        // Cache the list of support targets that we have.
        // If there is a new enrollment as we are syncing, it should/will not be processed until the next sync.
        SupportTargetEntity[] supportTargets = await dbContext.SupportTargets
            .ToArrayAsync(ct);

        foreach (SupportTargetEntity supportTarget in supportTargets)
        {
            try
            {
                await managersPerService[supportTarget.ServiceType]
                    .ForceSyncAllConsumersStatusIfSupported(supportTarget.Id);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Periodic consumer status sync failed for support target {SupportTargetId}",
                    supportTarget.Id
                );
            }
        }
    }
}