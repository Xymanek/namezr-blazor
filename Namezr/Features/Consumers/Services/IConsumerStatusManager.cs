using System.Diagnostics;
using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Consumers.Services;

public interface IConsumerStatusManager
{
    SupportServiceType ServiceType { get; }

    // TODO: some services do not support on demand status querying
    Task SyncConsumerStatus(Guid consumerId);

    Task<ICollection<UserSupportStatusEntry>> GetUserSupportStatuses(
        Guid consumerId, UserStatusSyncEagerness eagerness
    );
}

[AutoConstructor]
internal abstract partial class ConsumerStatusManagerBase : IConsumerStatusManager
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IClock _clock;

    public abstract SupportServiceType ServiceType { get; }

    public async Task<ICollection<UserSupportStatusEntry>> GetUserSupportStatuses(
        Guid consumerId, UserStatusSyncEagerness eagerness
    )
    {
        using Activity? activity = Diagnostics.ActivitySource.StartActivity()
            ?.AddTag("ConsumerId", consumerId);

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Keep in sync with QueryStatuses XML doc
        TargetConsumerEntity targetConsumer = await dbContext.TargetConsumers
            .AsSplitQuery()
            .AsTracking()
            .Include(x => x.SupportTarget.ServiceToken)
            .Include(x => x.SupportStatuses)
            .SingleAsync(x => x.Id == consumerId);

        Guard.IsTrue(targetConsumer.SupportTarget.ServiceType == ServiceType);

        bool syncNeeded = false;

        if (eagerness == UserStatusSyncEagerness.Force)
        {
            syncNeeded = true;
        }
        else if (eagerness > UserStatusSyncEagerness.NoSync)
        {
            if (
                // Force sync if we have no data about the consumer status
                targetConsumer.SupportStatuses!.Count < 1 &&
                
                // And there was no "all consumers sync" yet
                (
                    targetConsumer.SupportTarget.LastAllConsumersSyncAt == null ||
                    
                    // Or the last "all consumers sync" was too long ago
                    targetConsumer.SupportTarget.LastAllConsumersSyncAt < _clock.GetCurrentInstant() - DefaultOutdatedExpiration
                )
            )
            {
                syncNeeded = true;
            }
            else
            {
                syncNeeded = targetConsumer.SupportStatuses.Any(supportStatus =>
                    supportStatus.LastSyncedAt < _clock.GetCurrentInstant() - DefaultOutdatedExpiration
                );
            }
        }

        if (syncNeeded)
        {
            await ForceSyncConsumerStatus(targetConsumer);

            // Refresh the targetConsumer. TODO: optimize?
            targetConsumer = await dbContext.TargetConsumers
                .AsSplitQuery()
                .AsTracking()
                .Include(x => x.SupportTarget.ServiceToken)
                .Include(x => x.SupportStatuses)
                .SingleAsync(x => x.Id == consumerId);
        }

        List<UserSupportStatusEntry> result = new();
        foreach (ConsumerSupportStatusEntity supportStatus in targetConsumer.SupportStatuses!)
        {
            result.Add(new UserSupportStatusEntry
            {
                CreatorId = targetConsumer.SupportTarget.CreatorId,
                SupportPlanFullId = new SupportPlanFullId
                {
                    SupportTargetId = targetConsumer.SupportTarget.Id,
                    SupportPlanId = supportStatus.SupportPlanId,
                },
                SupportServiceType = targetConsumer.SupportTarget.ServiceType,
                SupportTargetServiceId = targetConsumer.SupportTarget.ServiceId,
                UserId = default, // TODO: oblivious here (user may not exist)
                ConsumerId = consumerId,
                ConsumerServiceId = targetConsumer.ServiceId,
                Data = new SupportStatusData
                {
                    IsActive = supportStatus.IsActive,
                    ExpiresAt = supportStatus.ExpiresAt,
                    EnrolledAt = supportStatus.EnrolledAt,
                },
                LastSyncedAt = supportStatus.LastSyncedAt,
            });
        }

        return result;
    }

    // TODO: think EXTREMELY hard about this, probably needs to be driven by subclasses
    private static readonly Duration DefaultOutdatedExpiration = Duration.FromHours(1);

    private async ValueTask ForceSyncConsumerStatus(TargetConsumerEntity consumer)
    {
        if (IndividualQuerySupported)
        {
            await SyncConsumerStatus(consumer.Id);
            return;
        }

        await ForceSyncAllConsumersStatus(consumer.SupportTargetId);
    }

    private async ValueTask ForceSyncAllConsumersStatus(Guid supportTargetId)
    {
        using Activity? activity = Diagnostics.ActivitySource.StartActivity()
            ?.AddTag("SupportTargetId", supportTargetId);

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SupportTargetEntity supportTarget = await dbContext.SupportTargets
            .AsTracking()
            .Include(x => x.ServiceToken)
            .Include(x => x.Consumers!).ThenInclude(x => x.SupportStatuses)
            .AsSplitQuery()
            .SingleAsync(x => x.Id == supportTargetId);

        Guard.IsTrue(supportTarget.ServiceType == ServiceType);

        Dictionary<string, Dictionary<string, SupportStatusData>> consumersStatuses
            = await QueryAllConsumersStatuses(supportTarget);

        // This will probably be every so slightly misaligned with the actual API response time but
        // [a] it's really hard to get the exact time due to all the latencies (network, processing)
        // [b] we mostly care about this for sake of not refreshing too often, so it's more important
        // to have the value once all requests are completed
        Instant checkedAt = _clock.GetCurrentInstant();

        Dictionary<string, TargetConsumerEntity> unprocessedConsumers = supportTarget.Consumers!
            .ToDictionary(x => x.ServiceId);

        foreach ((var consumerId, Dictionary<string, SupportStatusData> statusesInfo) in consumersStatuses)
        {
            if (!unprocessedConsumers.Remove(consumerId, out TargetConsumerEntity? targetConsumer))
            {
                targetConsumer = new TargetConsumerEntity
                {
                    ServiceId = consumerId,
                    SupportStatuses = [],
                };

                supportTarget.Consumers!.Add(targetConsumer);
            }

            StoreConsumerStatusInfo(targetConsumer, statusesInfo, checkedAt);
        }

        // Clear out any support statuses on consumers which were not returned.
        // We assume that they no longer exist or at least no longer support the target.
        foreach (TargetConsumerEntity targetConsumer in unprocessedConsumers.Values)
        {
            foreach (ConsumerSupportStatusEntity supportStatus in targetConsumer.SupportStatuses!)
            {
                supportStatus.LastSyncedAt = checkedAt;
                supportStatus.IsActive = false;
                supportStatus.ExpiresAt = null;
                supportStatus.EnrolledAt = null;
            }
        }

        supportTarget.LastAllConsumersSyncAt = checkedAt;

        await dbContext.SaveChangesAsync();
    }

    public async Task SyncConsumerStatus(Guid consumerId)
    {
        if (!IndividualQuerySupported)
        {
            // TODO: specific exception
            throw new NotSupportedException();
        }

        using Activity? activity = Diagnostics.ActivitySource.StartActivity()
            ?.AddTag("ConsumerId", consumerId);

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Keep in sync with QueryStatuses XML doc
        TargetConsumerEntity targetConsumer = await dbContext.TargetConsumers
            .AsSplitQuery()
            .AsTracking()
            .Include(x => x.SupportTarget.ServiceToken)
            .Include(x => x.SupportStatuses)
            .SingleAsync(x => x.Id == consumerId);

        Guard.IsTrue(targetConsumer.SupportTarget.ServiceType == ServiceType);

        Dictionary<string, SupportStatusData> statusesInfo = await QueryStatuses(targetConsumer);

        // This will probably be every so slightly misaligned with the actual API response time but
        // [a] it's really hard to get the exact time due to all the latencies (network, processing)
        // [b] we mostly care about this for sake of not refreshing too often, so it's more important
        // to have the value once all requests are completed
        Instant checkedAt = _clock.GetCurrentInstant();

        StoreConsumerStatusInfo(targetConsumer, statusesInfo, checkedAt);

        await dbContext.SaveChangesAsync();
    }

    private static void StoreConsumerStatusInfo(
        TargetConsumerEntity targetConsumer,
        Dictionary<string, SupportStatusData> statusesInfo,
        Instant checkedAt
    )
    {
        foreach (ConsumerSupportStatusEntity supportStatus in targetConsumer.SupportStatuses!)
        {
            if (statusesInfo.ContainsKey(supportStatus.SupportPlanId)) continue;

            // Definitely mark as inactive
            supportStatus.IsActive = false;
            supportStatus.ExpiresAt = null;
            supportStatus.EnrolledAt = null;
        }

        foreach ((string planId, SupportStatusData statusData) in statusesInfo)
        {
            ConsumerSupportStatusEntity? supportStatus = targetConsumer
                .SupportStatuses?
                .SingleOrDefault(x => x.SupportPlanId == planId);

            if (supportStatus is not null)
            {
                supportStatus.IsActive = statusData.IsActive;
                supportStatus.ExpiresAt = statusData.ExpiresAt;
                supportStatus.EnrolledAt = statusData.EnrolledAt;
            }
            else
            {
                supportStatus = new ConsumerSupportStatusEntity
                {
                    Consumer = targetConsumer,
                    SupportPlanId = planId,

                    IsActive = statusData.IsActive,
                    EnrolledAt = statusData.EnrolledAt,
                    ExpiresAt = statusData.ExpiresAt,

                    LastSyncedAt = checkedAt,
                };

                targetConsumer.SupportStatuses!.Add(supportStatus);
            }
        }

        // Mark all as updated
        foreach (ConsumerSupportStatusEntity supportStatus in targetConsumer.SupportStatuses!)
        {
            supportStatus.LastSyncedAt = checkedAt;
        }
    }

    protected abstract bool IndividualQuerySupported { get; }

    /// <summary>
    /// <para>Tracked entity - DO NOT MAKE ANY CHANGES, read only.</para>
    /// <para>The following navigations are loaded:</para>
    /// <list type="bullet">
    /// <item><see cref="P:Namezr.Features.Consumers.Data.TargetConsumerEntity.SupportTarget"/></item>
    /// <item>
    /// <see cref="P:Namezr.Features.Consumers.Data.TargetConsumerEntity.SupportTarget"/>
    /// -
    /// <see cref="P:Namezr.Features.Creators.Data.SupportTargetEntity.ServiceToken"/>
    /// </item>
    /// <item><see cref="P:Namezr.Features.Consumers.Data.TargetConsumerEntity.SupportStatuses"/></item>
    /// </list>
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown if <see cref="IndividualQuerySupported"/> is <see langword="false"/>.
    /// </exception>
    protected abstract ValueTask<Dictionary<string, SupportStatusData>> QueryStatuses(
        TargetConsumerEntity targetConsumer
    );

    protected abstract bool AllConsumersQuerySupported { get; }

    /// <returns>
    /// A dictionary where the key is the consumer  ID and the value is a dictionary.
    /// The inner dictionary is the same as <see cref="QueryStatuses"/>.
    /// </returns>
    protected abstract ValueTask<Dictionary<string, Dictionary<string, SupportStatusData>>> QueryAllConsumersStatuses(
        SupportTargetEntity supportTarget
    );
}