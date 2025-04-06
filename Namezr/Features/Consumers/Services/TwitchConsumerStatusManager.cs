using CommunityToolkit.Diagnostics;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators;
using Namezr.Features.Creators.Data;
using Namezr.Infrastructure.Twitch;
using TwitchLib.Api.Helix.Models.Channels.GetChannelFollowers;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Interfaces;

namespace Namezr.Features.Consumers.Services;

[AutoConstructor]
[RegisterSingleton(typeof(IConsumerStatusManager))]
internal partial class TwitchConsumerStatusManager : ConsumerStatusManagerBase
{
    private readonly ITwitchApiProvider _twitchApiProvider;

    public override SupportServiceType ServiceType => SupportServiceType.Twitch;

    protected override bool IndividualQuerySupported => true;

    protected override async ValueTask<ConsumerResult?> QueryStatuses(TargetConsumerEntity targetConsumer)
    {
        ITwitchAPI twitchApi = await _twitchApiProvider
            .GetTwitchApi(targetConsumer.SupportTarget.ServiceToken!);

        // Query the API in parallel

        Task<GetUserSubscriptionsResponse> subscriptionTask = twitchApi.Helix
            .Subscriptions
            .GetUserSubscriptionsAsync(
                broadcasterId: targetConsumer.SupportTarget.ServiceId,
                userIds: [targetConsumer.ServiceUserId]
            );

        Task<GetChannelFollowersResponse> followsTask = twitchApi.Helix
            .Channels
            .GetChannelFollowersAsync(
                broadcasterId: targetConsumer.SupportTarget.ServiceId,
                userId: targetConsumer.ServiceUserId
            );

        return UserDataToConsumerResult(
            (await subscriptionTask).Data.SingleOrDefault(),
            (await followsTask).Data.SingleOrDefault()
        );
    }

    private static readonly IReadOnlyList<string> TwitchSubscriptionTiers =
    [
        TwitchSupportPlansIds.Tier1,
        TwitchSupportPlansIds.Tier2,
        TwitchSupportPlansIds.Tier3,
    ];

    protected override bool AllConsumersQuerySupported => true;

    protected override async ValueTask<IReadOnlyCollection<ConsumerResult>> QueryAllConsumersStatuses(
        SupportTargetEntity supportTarget
    )
    {
        ITwitchAPI twitchApi = await _twitchApiProvider
            .GetTwitchApi(supportTarget.ServiceToken!);

        // TODO: Query the API in parallel
        
        List<Subscription> subscriptions = [];
        {
            GetBroadcasterSubscriptionsResponse? response = null;
            do
            {
                response = await twitchApi.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(
                    broadcasterId: supportTarget.ServiceId,
                    after: response?.Pagination.Cursor
                );

                subscriptions.AddRange(response.Data);
            } while (response is { Data.Length: > 0, Pagination.Cursor: not null });
        }

        List<ChannelFollower> followers = [];
        {
            GetChannelFollowersResponse? response = null;
            do
            {
                response = await twitchApi.Helix.Channels.GetChannelFollowersAsync(
                    broadcasterId: supportTarget.ServiceId,
                    after: response?.Pagination.Cursor
                );

                followers.AddRange(response.Data);
            } while (response is { Data.Length: > 0, Pagination.Cursor: not null });
        }

        Dictionary<string, Subscription> subscriptionsByUserId = subscriptions
            .ToDictionary(subscription => subscription.UserId);

        Dictionary<string, ChannelFollower> followersByUserId = followers
            .ToDictionary(follower => follower.UserId);

        return subscriptionsByUserId.Keys
            .Union(followersByUserId.Keys)
            .Select(userId => UserDataToConsumerResult(subscriptionsByUserId[userId], followersByUserId[userId]))
            .Where(x => x is not null)
            .ToArray()!;
    }

    private static ConsumerResult? UserDataToConsumerResult(Subscription? subscription, ChannelFollower? follower)
    {
        if (subscription is null && follower is null)
        {
            return null;
        }

        // Ensure we got the same user for both
        if (subscription is not null && follower is not null)
        {
            Guard.IsEqualTo(subscription.UserId, follower.UserId);
        }

        Dictionary<string, SupportStatusData> statuses = new();

        if (subscription is not null)
        {
            Guard.IsTrue(TwitchSubscriptionTiers.Contains(subscription.Tier));

            statuses.Add(subscription.Tier, new SupportStatusData
            {
                IsActive = true,

                // Twitch API does not provide this information - clear just in case it was set from a webhook?
                ExpiresAt = null,
                EnrolledAt = null,
            });
        }

        statuses.Add(TwitchSupportPlansIds.Follower, new SupportStatusData
        {
            IsActive = follower is not null,
            EnrolledAt = /*follower?.FollowedAt.ToInstant()*/ null, // TODO: parse string

            // Following never expires
            ExpiresAt = null,
        });

        return new ConsumerResult
        {
            ServiceUserId = (subscription?.UserId ?? follower?.UserId)!,
            RelationshipId = null, // There is no dedicated ID for "user in a specific channel"

            SupportPlanStatuses = statuses,
        };
    }
}