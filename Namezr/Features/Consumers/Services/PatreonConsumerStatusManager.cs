using System.Diagnostics.Contracts;
using AspNet.Security.OAuth.Patreon;
using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Features.Identity.Data;
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

    // The request just times out after no response from Patreon.
    // Note that if the auth or user ID is invalid, the API does instantly return an error.
    // Tested on 2025-03-15.
    protected override bool IndividualQuerySupported => false;

    /// <inheritdoc/>
    protected override async ValueTask<Dictionary<string, SupportStatusData>> QueryStatuses(
        TargetConsumerEntity targetConsumer
    )
    {
        // TODO
        CancellationToken ct = CancellationToken.None;
        
        
        // TODO: validate and lots of docs
        
        
        // TODO: this can be supported.
        // Patreon structures the data in the following way:
        // User <> Member(ship)          <> Campaign
        //                      <> Tier
        // With user's token we can call /identity and get the list of memberships - BUT NOT the tiers or the campaigns
        // However, knowing the member ID (GUID, different from the user ID) we can call /members/{memberId}
        // and that will give us campaign (to match the support target) and the tiers.
        // However, we probably also need to store the user ID <> member ID mapping in the database

        Member? membership;
        
        // If local ID is null 
        if (targetConsumer.RelationshipId is null)
        {
            membership = await FindMembershipAndAssociate(targetConsumer, ct);
        }
        else
        {
            PatreonClient patreonClientCreator = _patreonApiProvider.GetPatreonApi(
                targetConsumer.SupportTarget.ServiceToken!
            );
            
            membership = await FetchMembership(patreonClientCreator,targetConsumer.RelationshipId);
        }
        
        // If we didn't find a membership, the user is not a patron
        if (membership is null) return [];
        
        return BuildMemberResult(membership);
    }

    private async Task<Member?> FindMembershipAndAssociate(TargetConsumerEntity targetConsumer, CancellationToken ct)
    {
        Member? membership = await DoFindMembership(targetConsumer, ct);

        if (membership is not null)
        {
            await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

            TargetConsumerEntity updatedConsumer = await dbContext.TargetConsumers
                .AsTracking()
                .SingleAsync(x => x.Id == targetConsumer.Id, cancellationToken: ct);

            // Ensure that this consumer has not been associated with a different membership
            Guard.IsTrue(updatedConsumer.RelationshipId == null || updatedConsumer.RelationshipId == membership.Id);

            updatedConsumer.RelationshipId = membership.Id;
            await dbContext.SaveChangesAsync(ct);
        }

        return membership;
    }

    /// <summary>
    /// Brute forces the user's memberships to find the one for the current creator.
    /// </summary>
    private async Task<Member?> DoFindMembership(TargetConsumerEntity targetConsumer, CancellationToken ct)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        ApplicationUserLogin? userLogin = await dbContext.UserLogins
            .Where(login =>
                login.LoginProvider == PatreonAuthenticationDefaults.AuthenticationScheme &&
                login.ProviderKey == targetConsumer.ServiceId
            )
            .Include(x => x.ThirdPartyToken)
            .SingleOrDefaultAsync(ct);

        if (userLogin is null)
        {
            throw new InvalidOperationException(
                "This mechanism is only for use cases when a user exists with link to this Patreon account."
            );
        }

        PatreonClient patreonClientUser = _patreonApiProvider.GetPatreonApi( /* user token */ null!);
        User identity = await patreonClientUser.GetIdentityAsync(Includes.Memberships);

        PatreonClient patreonClientCreator = _patreonApiProvider.GetPatreonApi(
            targetConsumer.SupportTarget.ServiceToken!
        );

        IEnumerable<Task<Member?>> tasks = identity.Relationships.Memberships
            .Select(async membership =>
            {
                try
                {
                    return await FetchMembership(patreonClientCreator, membership.Id);
                }
                // What exception to catch?
                catch (Exception)
                {
                    // This membership is for a different creator
                    return null;
                }
            });

        return (await Task.WhenAll(tasks))
            .WhereNotNull()
            .SingleOrDefault();
    }

    private static Task<Member?> FetchMembership(PatreonClient patreonClientCreator, string memberId)
    {
        return patreonClientCreator.GetMemberAsync(memberId, Includes.Tiers);
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
            Dictionary<string, SupportStatusData> memberResult = BuildMemberResult(patreonMember);
            result.Add(patreonMember.Relationships.User.Id, memberResult);
        }

        return result;
    }

    private static Dictionary<string, SupportStatusData> BuildMemberResult(Member patreonMember)
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

        return memberResult;
    }
}

file static class EnumerableExtensions
{
    [Pure]
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class
    {
        // (ab)use of implementation detail
        return source.OfType<T>();
    }
}