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
                targetConsumer.SupportTarget.ServiceId,
                [targetConsumer.ServiceUserId]
            );

        Task<GetChannelFollowersResponse> followsTask = twitchApi.Helix
            .Channels
            .GetChannelFollowersAsync(
                broadcasterId: targetConsumer.SupportTarget.ServiceId,
                userId: targetConsumer.ServiceUserId
            );

        GetUserSubscriptionsResponse subscriptions = await subscriptionTask;

        // TODO
        // Instant? followedAt = (await followsTask).Data.SingleOrDefault()?.FollowedAt.ToInstant();
        bool following = (await followsTask).Data.SingleOrDefault() != null;

        Dictionary<string, SupportStatusData> statuses = new();

        // TODO: this approach currently does not create entities for non-current tiers - but do we need them?
        foreach (Subscription subscription in subscriptions.Data)
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
            IsActive = following,
            EnrolledAt = null,

            // Following never expires
            ExpiresAt = null,
        });

        return new ConsumerResult
        {
            ServiceUserId = targetConsumer.ServiceUserId,
            RelationshipId = null, // There is no dedicated ID for "user in a specific channel"

            SupportPlanStatuses = statuses,
        };
    }

    private static readonly IReadOnlyList<string> TwitchSubscriptionTiers =
    [
        TwitchSupportPlansIds.Tier1,
        TwitchSupportPlansIds.Tier2,
        TwitchSupportPlansIds.Tier3,
    ];

    protected override bool AllConsumersQuerySupported => false;

    protected override ValueTask<IReadOnlyCollection<ConsumerResult>> QueryAllConsumersStatuses(
        SupportTargetEntity supportTarget
    )
    {
        // TODO: implement
        throw new NotSupportedException();
    }
}