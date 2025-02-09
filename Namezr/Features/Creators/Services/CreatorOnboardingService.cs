using AspNet.Security.OAuth.Patreon;
using AspNet.Security.OAuth.Twitch;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;
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

        await foreach (Campaign campaign in await patreonApi.GetCampaignsAsync(userLogin.ProviderKey, Includes.Tiers))
        {
            // TODO: super suboptimal
            bool supportTargetAlreadyExists = await dbContext.SupportTargets
                .AnyAsync(x => x.ServiceType == SupportServiceType.Patreon && x.ServiceId == campaign.Id);

            if (supportTargetAlreadyExists) continue;

            targets.Add(new PotentialPatreonSupportTarget
            {
                CampaignId = campaign.Id,
                Title = campaign.CreationName,
                Url = campaign.Url,

                // TODO: what are benefits? Do I need them?
                Tiers = campaign.Relationships.Tiers
                    .Select(x => x.Title)
                    .ToArray(),
            });
        }

        return targets;
    }
}