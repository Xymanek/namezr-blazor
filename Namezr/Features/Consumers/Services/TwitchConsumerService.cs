using AspNet.Security.OAuth.Twitch;
using CommunityToolkit.Diagnostics;
using EntityFramework.Exceptions.Common;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators;
using Namezr.Features.Creators.Data;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.Twitch;
using NodaTime;
using NodaTime.Extensions;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.Interfaces;

namespace Namezr.Features.Consumers.Services;

[AutoConstructor]
[RegisterSingleton]
public partial class TwitchConsumerService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ITwitchApiProvider _twitchApiProvider;
    private readonly IClock _clock;

    public async Task SyncTwitchConsumerStatus(Guid consumerId)
    {
        // TODO: pull this logic apart so that the DB reading/writing part is shared (mostly?) for all service types
        // and the service-specific logic returns IAsyncEnumerable<some_record>
        // or Task<Dictionary<string, SupportStatusData>>
        
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        TargetConsumerEntity targetConsumer = await dbContext.TargetConsumers
            .AsSplitQuery()
            .AsTracking()
            .Include(x => x.SupportTarget.ServiceToken)
            .Include(x => x.SupportStatuses)
            .SingleAsync(x => x.Id == consumerId);

        Guard.IsTrue(targetConsumer.SupportTarget.ServiceType == SupportServiceType.Twitch);

        ITwitchAPI twitchApi = await _twitchApiProvider
            .GetTwitchApi(targetConsumer.SupportTarget.ServiceToken!);

        // TODO: check subscriptions and followers in parallel
        
        Instant checkedSubscriptionAt = _clock.GetCurrentInstant();
        CheckUserSubscriptionResponse subscriptionResponse = await twitchApi.Helix
            .Subscriptions
            .CheckUserSubscriptionAsync(
                targetConsumer.SupportTarget.ServiceId,
                targetConsumer.ServiceId
            );

        Instant checkedFollowAt = _clock.GetCurrentInstant();
        GetUsersFollowsResponse followResponse = await twitchApi.Helix.Users
            .GetUsersFollowsAsync(
                fromId: targetConsumer.ServiceId,
                toId: targetConsumer.SupportTarget.ServiceId
            );

        foreach (string tier in TwitchSubscriptionTiers)
        {
            bool isSubscribed = subscriptionResponse.Data.Any(x => x.Tier == tier);

            ConsumerSupportStatusEntity? supportStatus = targetConsumer
                .SupportStatuses?
                .SingleOrDefault(x => x.SupportPlanId == tier);

            if (supportStatus is not null)
            {
                supportStatus.IsActive = isSubscribed;
                supportStatus.LastSyncedAt = checkedSubscriptionAt;
            }
            else
            {
                supportStatus = new ConsumerSupportStatusEntity
                {
                    Consumer = targetConsumer,
                    SupportPlanId = tier,

                    IsActive = isSubscribed,
                    LastSyncedAt = checkedSubscriptionAt,
                };

                dbContext.ConsumerSupportStatus.Add(supportStatus);
            }

            // Twitch API does not provide this information - clear just in case it was set from a webhook?
            supportStatus.ExpiresAt = null;
            supportStatus.EnrolledAt = null;
        }

        Instant? followedAt = followResponse.Follows.SingleOrDefault()?.FollowedAt.ToInstant();

        ConsumerSupportStatusEntity? followStatus = targetConsumer
            .SupportStatuses?
            .SingleOrDefault(x => x.SupportPlanId == TwitchSupportTargetIds.Follower);

        if (followStatus is not null)
        {
            followStatus.IsActive = followedAt != null;
            followStatus.LastSyncedAt = checkedFollowAt;
        }
        else
        {
            followStatus = new ConsumerSupportStatusEntity
            {
                Consumer = targetConsumer,
                SupportPlanId = TwitchSupportTargetIds.Follower,

                IsActive = followedAt != null,
                LastSyncedAt = checkedFollowAt,
            };

            dbContext.ConsumerSupportStatus.Add(followStatus);
        }

        followStatus.ExpiresAt = null;
        followStatus.EnrolledAt = followedAt;

        await dbContext.SaveChangesAsync();
    }

    private static readonly IReadOnlyList<string> TwitchSubscriptionTiers =
    [
        TwitchSupportTargetIds.Tier1,
        TwitchSupportTargetIds.Tier2,
        TwitchSupportTargetIds.Tier3,
    ];

    public async Task<TargetConsumerEntity> GetOrCreateTwitchConsumer(Guid userId, Guid creatorId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SupportTargetEntity supportTarget = await dbContext.SupportTargets
            .SingleAsync(x => x.Id == creatorId && x.ServiceType == SupportServiceType.Twitch);

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .SingleAsync(x =>
                x.UserId == userId &&
                x.LoginProvider == TwitchAuthenticationDefaults.AuthenticationScheme
            );

        return await GetOrCreateTwitchConsumer(userLogin, supportTarget.Id);
    }

    public async Task<TargetConsumerEntity> GetOrCreateTwitchConsumer(
        ApplicationUserLogin userLogin,
        Guid supportTargetId
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        TargetConsumerEntity? entity = await dbContext.TargetConsumers
            .SingleOrDefaultAsync(x => x.ServiceId == userLogin.ProviderKey && x.SupportTargetId == supportTargetId);

        if (entity is not null)
        {
            return entity;
        }

        try
        {
            entity = new TargetConsumerEntity
            {
                SupportTargetId = supportTargetId,
                ServiceId = userLogin.ProviderKey,
            };

            dbContext.TargetConsumers.Add(entity);
            await dbContext.SaveChangesAsync();

            return entity;
        }
        catch (UniqueConstraintException)
        {
            // Race, it was already created

            return await dbContext.TargetConsumers
                .SingleAsync(x => x.ServiceId == userLogin.ProviderKey && x.SupportTargetId == supportTargetId);
        }
    }
}