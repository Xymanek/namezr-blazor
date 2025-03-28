﻿using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
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

    // The request just times out after no response from Patreon.
    // Note that if the auth or user ID is invalid, the API does instantly return an error.
    // Tested on 2025-03-15.
    protected override bool IndividualQuerySupported => false;

    /// <inheritdoc/>
    protected override ValueTask<Dictionary<string, SupportStatusData>> QueryStatuses(
        TargetConsumerEntity targetConsumer
    )
    {
        // TODO: this can be supported.
        // Patreon structures the data in the following way:
        // User <> Member(ship)          <> Campaign
        //                      <> Tier
        // With user's token we can call /identity and get the list of memberships - BUT NOT the tiers or the campaigns
        // However, knowing the member ID (GUID, different from the user ID) we can call /members/{memberId}
        // and that will give us campaign (to match the support target) and the tiers.
        // However, we probably also need to store the user ID <> member ID mapping in the database

        throw new NotSupportedException();
    }

    protected override bool AllConsumersQuerySupported => true;

    // TODO: loaded relationships
    protected override async ValueTask<Dictionary<string, Dictionary<string, SupportStatusData>>>
        QueryAllConsumersStatuses(
            SupportTargetEntity supportTarget
        )
    {
        PatreonClient patreonClient = _patreonApiProvider.GetPatreonApi(supportTarget.ServiceToken!);

        PatreonResourceArray<Member, MemberRelationships> resourceArray =
            await patreonClient.GetCampaignMembersAsync(supportTarget.ServiceId,
                Includes.CurrentlyEntitledTiers | Includes.User);

        Dictionary<string, Dictionary<string, SupportStatusData>> result = new();

        await foreach (Member patreonMember in resourceArray)
        {
            Dictionary<string, SupportStatusData> memberResult = new();

            foreach (Tier patreonTier in patreonMember.Relationships.Tiers)
            {
                memberResult[patreonTier.Id] = new SupportStatusData
                {
                    IsActive = true,
                    ExpiresAt = patreonMember.NextChargeDate?.ToInstant(),
                    EnrolledAt = patreonMember.PledgeRelationshipStart?.ToInstant(),
                };
            }

            result.Add(patreonMember.Relationships.User.Id, memberResult);
        }

        return result;
    }
}