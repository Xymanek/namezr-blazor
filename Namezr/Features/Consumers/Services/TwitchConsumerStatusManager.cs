using CommunityToolkit.Diagnostics;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators;
using Namezr.Infrastructure.Twitch;
using NodaTime;
using NodaTime.Extensions;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.Users.GetUserFollows;
using TwitchLib.Api.Interfaces;

namespace Namezr.Features.Consumers.Services;

[AutoConstructor]
[RegisterSingleton(typeof(IConsumerStatusManager))]
internal partial class TwitchConsumerStatusManager : ConsumerStatusManagerBase
{
    private readonly ITwitchApiProvider _twitchApiProvider;

    public override SupportServiceType ServiceType => SupportServiceType.Twitch;

    protected override async ValueTask<Dictionary<string, SupportStatusData>> QueryStatuses(
        TargetConsumerEntity targetConsumer
    )
    {
        ITwitchAPI twitchApi = await _twitchApiProvider
            .GetTwitchApi(targetConsumer.SupportTarget.ServiceToken!);

        // Query the API in parallel

        Task<CheckUserSubscriptionResponse> subscriptionTask = twitchApi.Helix
            .Subscriptions
            .CheckUserSubscriptionAsync(
                targetConsumer.SupportTarget.ServiceId,
                targetConsumer.ServiceId
            );

        Task<GetUsersFollowsResponse> followsTask = twitchApi.Helix
            .Users
            .GetUsersFollowsAsync(
                fromId: targetConsumer.ServiceId,
                toId: targetConsumer.SupportTarget.ServiceId
            );

        CheckUserSubscriptionResponse subscriptions = await subscriptionTask;
        Instant? followedAt = (await followsTask).Follows.SingleOrDefault()?.FollowedAt.ToInstant();

        Dictionary<string, SupportStatusData> result = new();

        foreach (Subscription subscription in subscriptions.Data)
        {
            Guard.IsTrue(TwitchSubscriptionTiers.Contains(subscription.Tier));

            result.Add(subscription.Tier, new SupportStatusData
            {
                IsActive = true,

                // Twitch API does not provide this information - clear just in case it was set from a webhook?
                ExpiresAt = null,
                EnrolledAt = null,
            });
        }

        result.Add(TwitchSupportPlansIds.Follower, new SupportStatusData
        {
            IsActive = followedAt != null,
            EnrolledAt = followedAt,

            // Following never expires
            ExpiresAt = null,
        });

        return result;
    }

    private static readonly IReadOnlyList<string> TwitchSubscriptionTiers =
    [
        TwitchSupportPlansIds.Tier1,
        TwitchSupportPlansIds.Tier2,
        TwitchSupportPlansIds.Tier3,
    ];
}