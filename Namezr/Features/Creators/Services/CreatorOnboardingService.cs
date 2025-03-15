using AspNet.Security.OAuth.Patreon;
using AspNet.Security.OAuth.Twitch;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Creators.Data;
using Namezr.Features.Creators.Pages;
using Namezr.Features.Identity.Data;
using Namezr.Features.ThirdParty;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.Patreon;
using Namezr.Infrastructure.Twitch;
using Patreon.Net;
using Patreon.Net.Models;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Interfaces;
using User = TwitchLib.Api.Helix.Models.Users.GetUsers.User;

namespace Namezr.Features.Creators.Services;

internal interface ICreatorOnboardingService
{
    Task<IReadOnlyList<PotentialSupportTarget>> GetPotentialSupportTargets(Guid userId);

    Task<CreatorEntity> CreateCreator(
        PotentialSupportTarget initialSupportTarget,
        CreatorOnboardingModel validatedModel,
        ApplicationUser owner
    );
}

[AutoConstructor]
[RegisterSingleton]
internal partial class CreatorOnboardingService : ICreatorOnboardingService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IPatreonApiProvider _patreonApiProvider;
    private readonly ITwitchApiProvider _twitchApiProvider;

    public async Task<IReadOnlyList<PotentialSupportTarget>> GetPotentialSupportTargets(Guid userId)
    {
        List<PotentialSupportTarget> targets = new();

        PotentialTwitchSupportTarget? targetTwitch = await GetPotentialSupportTargetTwitch(userId);
        if (targetTwitch is not null) targets.Add(targetTwitch);

        targets.AddRange(await GetPotentialSupportTargetsPatreon(userId));

        return targets;
    }

    private async Task<PotentialTwitchSupportTarget?> GetPotentialSupportTargetTwitch(Guid userId)
    {
        // TODO: figure out the API design for fetching user's connections, API, etc.
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        ApplicationUserLogin? userLogin = await dbContext.UserLogins
            .SingleOrDefaultAsync(x =>
                x.UserId == userId &&
                x.LoginProvider == TwitchAuthenticationDefaults.AuthenticationScheme
            );

        if (userLogin is null) return null;

        string userTwitchId = userLogin.ProviderKey;

        bool supportTargetAlreadyExists = await dbContext.SupportTargets
            .AnyAsync(x => x.ServiceType == SupportServiceType.Twitch && x.ServiceId == userTwitchId);

        if (supportTargetAlreadyExists) return null;

        ITwitchAPI twitchApi = await _twitchApiProvider.GetTwitchApiForUser(userId);

        GetUsersResponse response = await twitchApi.Helix.Users.GetUsersAsync(ids: [userTwitchId]);
        User userInfo = response.Users.Single();

        return new PotentialTwitchSupportTarget
        {
            ThirdPartyTokenId = userLogin.ThirdPartyTokenId,
            UserTwitchId = userTwitchId,
            TwitchDisplayName = userInfo.DisplayName,
            TwitchProfileUrl = userInfo.ProfileImageUrl,
            BroadcasterType = userInfo.BroadcasterType,
        };
    }

    private async Task<IReadOnlyList<PotentialPatreonSupportTarget>> GetPotentialSupportTargetsPatreon(Guid userId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        ApplicationUserLogin? userLogin = await dbContext.UserLogins
            .SingleOrDefaultAsync(x =>
                x.UserId == userId &&
                x.LoginProvider == PatreonAuthenticationDefaults.AuthenticationScheme
            );

        if (userLogin is null) return Array.Empty<PotentialPatreonSupportTarget>();

        using PatreonClient patreonApi = await _patreonApiProvider.GetPatreonApiForUser(userId);
        List<PotentialPatreonSupportTarget> targets = new();

        await foreach (Campaign campaign in await patreonApi.GetCampaignsAsync(Includes.Tiers))
        {
            // TODO: super suboptimal
            bool supportTargetAlreadyExists = await dbContext.SupportTargets
                .AnyAsync(x => x.ServiceType == SupportServiceType.Patreon && x.ServiceId == campaign.Id);

            if (supportTargetAlreadyExists) continue;

            targets.Add(new PotentialPatreonSupportTarget
            {
                ThirdPartyTokenId = userLogin.ThirdPartyTokenId,

                CampaignId = campaign.Id,
                Title = campaign.Vanity,
                Url = campaign.Url,

                Tiers = campaign.Relationships.Tiers
                    .Select(x => x.Title)
                    .ToArray(),
            });
        }

        return targets;
    }

    public async Task<CreatorEntity> CreateCreator(
        PotentialSupportTarget initialSupportTarget,
        CreatorOnboardingModel validatedModel,
        ApplicationUser owner
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SupportTargetEntity supportTarget = new()
        {
            ServiceType = initialSupportTarget.ServiceType,
            ServiceId = initialSupportTarget.ServiceId,

            OwningStaffMemberId = owner.Id,
            ServiceTokenId = initialSupportTarget.ThirdPartyTokenId,
        };

        await PopulateSupportTarget(supportTarget, initialSupportTarget);

        CreatorEntity creator = new()
        {
            DisplayName = validatedModel.CreatorName!,

            SupportTargets = [supportTarget],
            Staff =
            [
                new CreatorStaffEntity
                {
                    UserId = owner.Id,
                    OwnedSupportTargets = [supportTarget],
                }
            ],
        };

        dbContext.Creators.Add(creator);
        await dbContext.SaveChangesAsync();

        return creator;
    }

    private async ValueTask PopulateSupportTarget(
        SupportTargetEntity supportTarget,
        PotentialSupportTarget potentialSupportTarget
    )
    {
        if (potentialSupportTarget is PotentialPatreonSupportTarget patreonTarget)
        {
            await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

            ThirdPartyToken token = await dbContext.ThirdPartyTokens
                .SingleAsync(x => x.Id == potentialSupportTarget.ThirdPartyTokenId);

            PatreonClient patreonClient = _patreonApiProvider.GetPatreonApi(token);
            Campaign campaign = await patreonClient.GetCampaignAsync(patreonTarget.CampaignId, Includes.Tiers);
            
            supportTarget.SupportPlansInfos ??= new HashSet<SupportPlanInfoEntity>();
            foreach (Tier tier in campaign.Relationships.Tiers)
            {
                supportTarget.SupportPlansInfos.Add(new SupportPlanInfoEntity
                {
                    SupportPlanId = tier.Id,
                    DisplayName = tier.Title,
                });
            }
        }
    }
}