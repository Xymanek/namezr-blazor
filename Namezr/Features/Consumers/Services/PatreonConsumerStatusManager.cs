using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Infrastructure.Patreon;
using NodaTime.Extensions;
using Patreon.Net;
using Patreon.Net.Models;

namespace Namezr.Features.Consumers.Services;

[AutoConstructor]
[RegisterSingleton(typeof(IConsumerStatusManager))]
internal partial class PatreonConsumerStatusManager : ConsumerStatusManagerBase
{
    private readonly IPatreonApiProvider _patreonApiProvider;

    public override SupportServiceType ServiceType => SupportServiceType.Patreon;

    /// <inheritdoc/>
    protected override async ValueTask<Dictionary<string, SupportStatusData>> QueryStatuses(
        TargetConsumerEntity targetConsumer
    )
    {
        PatreonClient patreonClient = _patreonApiProvider.GetPatreonApi(targetConsumer.SupportTarget.ServiceToken!);
        Member patreonMember = await patreonClient.GetMemberAsync(targetConsumer.ServiceId);

        Dictionary<string, SupportStatusData> result = new();

        foreach (Tier patreonTier in patreonMember.Relationships.Tiers)
        {
            result[patreonTier.Id] = new SupportStatusData
            {
                IsActive = true,
                ExpiresAt = patreonMember.NextChargeDate?.ToInstant(),
                EnrolledAt = patreonMember.PledgeRelationshipStart?.ToInstant(),
            };
        }

        return result;
    }
}