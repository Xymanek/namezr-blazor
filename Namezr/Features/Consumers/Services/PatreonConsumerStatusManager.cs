using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.Patreon;
using NodaTime.Extensions;
using Patreon.Net;
using Patreon.Net.Models;

namespace Namezr.Features.Consumers.Services;

[AutoConstructor]
[RegisterSingleton(typeof(IConsumerStatusManager))]
internal partial class PatreonConsumerStatusManager : ConsumerStatusManagerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IPatreonApiProvider _patreonApiProvider;

    public override SupportServiceType ServiceType => SupportServiceType.Patreon;

    protected override bool IndividualQuerySupported => true;

    /// <inheritdoc/>
    protected override async ValueTask<ConsumerResult?> QueryStatuses(TargetConsumerEntity targetConsumer)
    {
        // TODO: docs about flow
        
        // If relationship/member(ship) ID is null, then we need to query
        // all memberships and find the one for the current user.
        if (targetConsumer.RelationshipId is null)
        {
            throw new SyncAllRequired();
        }

        PatreonClient patreonClientCreator = _patreonApiProvider.GetPatreonApi(
            targetConsumer.SupportTarget.ServiceToken!
        );

        Member? membership = await patreonClientCreator.GetMemberAsync(
            targetConsumer.RelationshipId,
            Includes.User | Includes.CurrentlyEntitledTiers
        );

        // If we didn't find a membership, the user is no longer a patron
        if (membership is null) return null;

        return BuildMemberResult(membership);
    }

    protected override bool AllConsumersQuerySupported => true;

    // TODO: loaded relationships docs
    protected override async ValueTask<IReadOnlyCollection<ConsumerResult>> QueryAllConsumersStatuses(
        SupportTargetEntity supportTarget
    )
    {
        PatreonClient patreonClient = _patreonApiProvider.GetPatreonApi(supportTarget.ServiceToken!);

        PatreonResourceArray<Member, MemberRelationships> resourceArray = await patreonClient.GetCampaignMembersAsync(
            supportTarget.ServiceId,
            Includes.User | Includes.CurrentlyEntitledTiers
        );

        return await resourceArray
            .Select(BuildMemberResult)
            .ToListAsync();
    }

    private static ConsumerResult BuildMemberResult(Member patreonMember)
    {
        Dictionary<string, SupportStatusData> statuses = new();

        foreach (Tier patreonTier in patreonMember.Relationships.Tiers)
        {
            statuses[patreonTier.Id] = new SupportStatusData
            {
                IsActive = true,
                ExpiresAt = patreonMember.NextChargeDate?.ToInstant(),
                EnrolledAt = patreonMember.PledgeRelationshipStart?.ToInstant(),
            };
        }

        return new ConsumerResult
        {
            ServiceUserId = patreonMember.Relationships.User.Id,
            RelationshipId = patreonMember.Id,

            SupportPlanStatuses = statuses,
        };
    }
}