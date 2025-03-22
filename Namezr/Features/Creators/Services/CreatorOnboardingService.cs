using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Patreon;
using AspNet.Security.OAuth.Twitch;
using Discord;
using Discord.Rest;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Creators.Data;
using Namezr.Features.Creators.Pages;
using Namezr.Features.Identity.Data;
using Namezr.Features.ThirdParty;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.Discord;
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

    Task AddTargetToCreator(
        PotentialSupportTarget initialSupportTarget,
        Guid creatorId,
        ApplicationUser targetOwner
    );
}

[AutoConstructor]
[RegisterSingleton]
internal partial class CreatorOnboardingService : ICreatorOnboardingService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IPatreonApiProvider _patreonApiProvider;
    private readonly IDiscordApiProvider _discordApiProvider;
    private readonly ITwitchApiProvider _twitchApiProvider;

    public async Task<IReadOnlyList<PotentialSupportTarget>> GetPotentialSupportTargets(Guid userId)
    {
        List<PotentialSupportTarget> targets = new();

        PotentialTwitchSupportTarget? targetTwitch = await GetPotentialSupportTargetTwitch(userId);
        if (targetTwitch is not null) targets.Add(targetTwitch);

        targets.AddRange(await GetPotentialSupportTargetsPatreon(userId));
        targets.AddRange(await GetPotentialSupportTargetsDiscord(userId));

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

    private async Task<IReadOnlyList<PotentialDiscordSupportTarget>> GetPotentialSupportTargetsDiscord(Guid userId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
        ApplicationUserLogin? userLogin = await dbContext.UserLogins
            .SingleOrDefaultAsync(x =>
                x.UserId == userId &&
                x.LoginProvider == DiscordAuthenticationDefaults.AuthenticationScheme
            );

        if (userLogin is null) return [];

        // TODO: there is not a single line here that is efficient

        await using DiscordRestClient discordUserClient = await _discordApiProvider.GetDiscordApiForUser(userId);
        await using DiscordRestClient discordAppClient = await _discordApiProvider.GetDiscordApiForApp();

        Dictionary<ulong, RestGuild> appJoinedGuilds = (await discordAppClient.GetGuildsAsync())
            .ToDictionary(guild => guild.Id);

        List<PotentialDiscordSupportTarget> targets = new();
        await foreach (RestUserGuild? guild in discordUserClient.GetGuildSummariesAsync().Flatten())
        {
            // Use must have ability to add bots
            if (!guild.Permissions.ManageGuild) continue;

            string guildId = guild.Permissions.ToString();

            // TODO: super suboptimal
            bool supportTargetAlreadyExists = await dbContext.SupportTargets
                .AnyAsync(x => x.ServiceType == SupportServiceType.Patreon && x.ServiceId == guildId);

            if (supportTargetAlreadyExists) continue;

            string[]? roleNames = null;

            if (appJoinedGuilds.TryGetValue(guild.Id, out RestGuild? appJoinedGuild))
            {
                roleNames = appJoinedGuild.Roles
                    .Select(x => x.Name)
                    .ToArray();
            }

            targets.Add(new PotentialDiscordSupportTarget
            {
                // Discord info is gathered via bot token, not user oauth
                ThirdPartyTokenId = null,

                GuildId = guild.Id,
                GuildName = guild.Name,

                BotInstallRequired = roleNames == null,
                RoleNames = roleNames ?? [],
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

        SupportTargetEntity supportTarget = await SetupTarget(initialSupportTarget, owner);

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

    public async Task AddTargetToCreator(
        PotentialSupportTarget initialSupportTarget,
        Guid creatorId,
        ApplicationUser targetOwner
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        CreatorEntity creator = await dbContext.Creators
            .AsTracking()
            .Include(x => x.Staff)
            .SingleAsync(x => x.Id == creatorId);

        CreatorStaffEntity? staffEntity = creator.Staff!.SingleOrDefault(staff => staff.UserId == targetOwner.Id);

        if (staffEntity == null)
        {
            throw new Exception("Target owner must be creator staff already");
        }

        SupportTargetEntity supportTarget = await SetupTarget(initialSupportTarget, targetOwner);
        supportTarget.StaffEntry = staffEntity;
        supportTarget.Creator = creator;

        dbContext.SupportTargets.Add(supportTarget);
        await dbContext.SaveChangesAsync();
    }

    private async Task<SupportTargetEntity> SetupTarget(
        PotentialSupportTarget initialSupportTarget,
        ApplicationUser targetOwner
    )
    {
        SupportTargetEntity supportTarget = new()
        {
            ServiceType = initialSupportTarget.ServiceType,
            ServiceId = initialSupportTarget.ServiceId,

            OwningStaffMemberId = targetOwner.Id,
            ServiceTokenId = initialSupportTarget.ThirdPartyTokenId,
        };

        await PopulateSupportTarget(supportTarget, initialSupportTarget);

        return supportTarget;
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

        if (potentialSupportTarget is PotentialDiscordSupportTarget discordTarget)
        {
            await using DiscordRestClient discordAppClient = await _discordApiProvider.GetDiscordApiForApp();

            RestGuild guild = await discordAppClient.GetGuildAsync(discordTarget.GuildId);

            supportTarget.SupportPlansInfos ??= new HashSet<SupportPlanInfoEntity>();
            foreach (RestRole role in guild.Roles)
            {
                supportTarget.SupportPlansInfos.Add(new SupportPlanInfoEntity
                {
                    SupportPlanId = role.Id.ToString(),
                    DisplayName = role.Name,
                });
            }
        }
    }
}