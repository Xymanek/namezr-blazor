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

    Task<ICollection<UserSupportStatusEntry>> GetUserSupportStatuses(
        Guid consumerId, UserStatusSyncEagerness eagerness
    );

    Task ForceSyncAllConsumersStatusIfSupported(Guid supportTargetId);
}

[AutoConstructor]
internal abstract partial class ConsumerStatusManagerBase : IConsumerStatusManager
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<ConsumerStatusManagerBase> _logger;
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
            if (targetConsumer.LastSyncedAt == null)
            {
                // Force sync if we have no data about the consumer status
                syncNeeded = true;
            }
            else
            {
                syncNeeded = targetConsumer.LastSyncedAt < _clock.GetCurrentInstant() - DefaultOutdatedExpiration;
            }
        }

        if (syncNeeded)
        {
            await ForceSyncConsumerStatus(targetConsumer);

            // Refresh the targetConsumer. TODO: optimize?
            targetConsumer = await dbContext.TargetConsumers
                .AsSplitQuery()
                .AsNoTrackingWithIdentityResolution() // Important, otherwise we'll just get the old entities 
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
                ConsumerServiceId = targetConsumer.ServiceUserId,
                Data = new SupportStatusData
                {
                    IsActive = supportStatus.IsActive,
                    ExpiresAt = supportStatus.ExpiresAt,
                    EnrolledAt = supportStatus.EnrolledAt,
                },
                LastSyncedAt = targetConsumer.LastSyncedAt!.Value,
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
            try
            {
                await DoSyncIndividualConsumer(consumer.Id);
                return;
            }
            catch (SyncAllRequired)
            {
                LogSyncAllRequired(consumer.Id);
                // Fall through to ForceSyncAllConsumersStatus
            }
        }

        await ForceSyncAllConsumersStatus(consumer.SupportTargetId);
    }

    public async Task ForceSyncAllConsumersStatusIfSupported(Guid supportTargetId)
    {
        if (!AllConsumersQuerySupported) return;

        await ForceSyncAllConsumersStatus(supportTargetId);
    }

    public async ValueTask ForceSyncAllConsumersStatus(Guid supportTargetId)
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

        IReadOnlyCollection<ConsumerResult> consumersResults
            = await QueryAllConsumersStatuses(supportTarget);

        // This will probably be every so slightly misaligned with the actual API response time but
        // [a] it's really hard to get the exact time due to all the latencies (network, processing)
        // [b] we mostly care about this for sake of not refreshing too often, so it's more important
        // to have the value once all requests are completed
        Instant checkedAt = _clock.GetCurrentInstant();

        Dictionary<string, TargetConsumerEntity> consumersByServiceUserId = supportTarget.Consumers!
            .ToDictionary(x => x.ServiceUserId);

        Dictionary<string, TargetConsumerEntity> consumersByRelationshipId = supportTarget.Consumers!
            .Where(x => x.RelationshipId is not null)
            .ToDictionary(x => x.RelationshipId!);

        HashSet<TargetConsumerEntity> unprocessedConsumers = supportTarget.Consumers!.ToHashSet();

        foreach (ConsumerResult result in consumersResults)
        {
            TargetConsumerEntity? targetConsumer = null;

            if (result.RelationshipId is not null)
            {
                consumersByRelationshipId.TryGetValue(result.RelationshipId, out targetConsumer);
            }

            if (targetConsumer is null)
            {
                consumersByServiceUserId.TryGetValue(result.ServiceUserId, out targetConsumer);
            }

            if (targetConsumer != null)
            {
                unprocessedConsumers.Remove(targetConsumer);
            }
            else
            {
                targetConsumer = new TargetConsumerEntity
                {
                    ServiceUserId = result.ServiceUserId,
                    RelationshipId = result.RelationshipId,

                    SupportStatuses = [],
                };

                supportTarget.Consumers!.Add(targetConsumer);
                consumersByServiceUserId.Add(targetConsumer.ServiceUserId, targetConsumer);

                if (targetConsumer.RelationshipId != null)
                {
                    consumersByRelationshipId.Add(targetConsumer.RelationshipId, targetConsumer);
                }
            }

            StoreConsumerResult(targetConsumer, result, checkedAt);
        }

        foreach (TargetConsumerEntity targetConsumer in unprocessedConsumers)
        {
            StoreConsumerResult(targetConsumer, null, checkedAt);
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task DoSyncIndividualConsumer(Guid consumerId)
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

        ConsumerResult? consumerResult = await QueryStatuses(targetConsumer);

        // This will probably be every so slightly misaligned with the actual API response time but
        // [a] it's really hard to get the exact time due to all the latencies (network, processing)
        // [b] we mostly care about this for sake of not refreshing too often, so it's more important
        // to have the value once all requests are completed
        Instant checkedAt = _clock.GetCurrentInstant();

        StoreConsumerResult(targetConsumer, consumerResult, checkedAt);

        await dbContext.SaveChangesAsync();
    }

    /// <param name="targetConsumer"></param>
    /// <param name="result">If null, the consumer is assumed to be gone.</param>
    /// <param name="checkedAt"></param>
    private static void StoreConsumerResult(
        TargetConsumerEntity targetConsumer,
        ConsumerResult? result,
        Instant checkedAt
    )
    {
        // First, mark as updated
        targetConsumer.LastSyncedAt = checkedAt;

        // Then, bail if we got nothing
        if (result == null)
        {
            // Clear out any support statuses on consumers which were not returned.
            // We assume that they no longer exist or at least no longer support the target.
            foreach (ConsumerSupportStatusEntity supportStatus in targetConsumer.SupportStatuses!)
            {
                supportStatus.IsActive = false;
                supportStatus.ExpiresAt = null;
                supportStatus.EnrolledAt = null;
            }

            return;
        }

        // Ensure we got the right user
        Guard.IsEqualTo(targetConsumer.ServiceUserId, result.ServiceUserId);

        if (result.RelationshipId is null)
        {
            // If we got no relationship ID then ensure the stored record also has none
            // There is currently no known scenario where a relationship ID "disappears"
            Guard.IsNull(targetConsumer.RelationshipId);
        }
        else
        {
            // Store the relationship ID if we did not already know it (this happens with Patreon)
            if (targetConsumer.RelationshipId is null)
            {
                targetConsumer.RelationshipId = result.RelationshipId;
            }

            // If we already had a relationship ID, ensure it matches
            else
            {
                Guard.IsEqualTo(targetConsumer.RelationshipId, result.RelationshipId);
            }
        }

        IReadOnlyDictionary<string, SupportStatusData> statusesInfo = result.SupportPlanStatuses;

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
                };

                targetConsumer.SupportStatuses!.Add(supportStatus);
            }
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
    protected abstract ValueTask<ConsumerResult?> QueryStatuses(
        TargetConsumerEntity targetConsumer
    );

    /// <summary>
    /// To be thrown ONLY BY <see cref="ConsumerStatusManagerBase.QueryStatuses"/>.
    /// </summary>
    protected class SyncAllRequired : Exception;

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "SyncAllRequired for consumer {ConsumerId}"
    )]
    private partial void LogSyncAllRequired(Guid consumerId);

    protected abstract bool AllConsumersQuerySupported { get; }

    /// <returns>
    /// A dictionary where the key is the consumer  ID and the value is a dictionary.
    /// The inner dictionary is the same as <see cref="QueryStatuses"/>.
    /// </returns>
    protected abstract ValueTask<IReadOnlyCollection<ConsumerResult>> QueryAllConsumersStatuses(
        SupportTargetEntity supportTarget
    );

    protected class ConsumerResult
    {
        public required string ServiceUserId { get; init; }
        public required string? RelationshipId { get; init; }

        public required IReadOnlyDictionary<string, SupportStatusData> SupportPlanStatuses { get; init; }
    }
}