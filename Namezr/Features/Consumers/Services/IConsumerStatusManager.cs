using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Consumers.Services;

public interface IConsumerStatusManager
{
    SupportServiceType ServiceType { get; }

    // TODO: some services do not support on demand status querying
    Task SyncConsumerStatus(Guid consumerId);
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
            if (targetConsumer.SupportStatuses!.Count < 1)
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
            // TODO
            
            // + refetch the targetConsumer.SupportStatuses or return from the sync?
        }
        
        List<UserSupportStatusEntry> result = new();
        foreach (ConsumerSupportStatusEntity supportStatus in targetConsumer.SupportStatuses!)
        {
            result.Add(new UserSupportStatusEntry
            {
                CreatorId = targetConsumer.SupportTarget.CreatorId,
                SupportTargetId = targetConsumer.SupportTarget.Id,
                SupportServiceType = targetConsumer.SupportTarget.ServiceType,
                SupportTargetServiceId = targetConsumer.SupportTarget.ServiceId,
                SupportPlanId = supportStatus.SupportPlanId,
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

    public async Task SyncConsumerStatus(Guid consumerId)
    {
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

                dbContext.ConsumerSupportStatus.Add(supportStatus);
            }
        }

        // Mark all as updated
        foreach (ConsumerSupportStatusEntity supportStatus in targetConsumer.SupportStatuses!)
        {
            supportStatus.LastSyncedAt = checkedAt;
        }

        await dbContext.SaveChangesAsync();
    }

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
    protected abstract ValueTask<Dictionary<string, SupportStatusData>> QueryStatuses(
        TargetConsumerEntity targetConsumer
    );
}