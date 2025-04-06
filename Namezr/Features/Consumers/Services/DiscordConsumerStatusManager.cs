using Discord;
using Discord.Rest;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Infrastructure.Discord;

namespace Namezr.Features.Consumers.Services;

[AutoConstructor]
[RegisterSingleton(typeof(IConsumerStatusManager))]
internal partial class DiscordConsumerStatusManager : ConsumerStatusManagerBase
{
    private readonly IDiscordApiProvider _discordApiProvider;

    public override SupportServiceType ServiceType => SupportServiceType.Discord;

    protected override bool IndividualQuerySupported => true;

    /// <inheritdoc/>
    protected override async ValueTask<ConsumerResult?> QueryStatuses(TargetConsumerEntity targetConsumer)
    {
        await using DiscordRestClient discordClient = await _discordApiProvider.GetDiscordApiForApp();

        RestGuildUser? guildUser = await discordClient.GetGuildUserAsync(
            guildId: ulong.Parse(targetConsumer.SupportTarget.ServiceId),
            id: ulong.Parse(targetConsumer.ServiceUserId)
        );

        if (guildUser is null)
        {
            return null;
        }

        return GuildUserToConsumerResult(guildUser);
    }

    protected override bool AllConsumersQuerySupported => true;

    protected override async ValueTask<IReadOnlyCollection<ConsumerResult>> QueryAllConsumersStatuses(
        SupportTargetEntity supportTarget
    )
    {
        await using DiscordRestClient discordClient = await _discordApiProvider.GetDiscordApiForApp();

        RestGuild guild = await discordClient.GetGuildAsync(ulong.Parse(supportTarget.ServiceId));

        return await guild.GetUsersAsync()
            .Flatten()
            .Select(GuildUserToConsumerResult)
            .ToArrayAsync();
    }

    private static ConsumerResult GuildUserToConsumerResult(RestGuildUser guildUser)
    {
        return new ConsumerResult
        {
            ServiceUserId = guildUser.Id.ToString(),
            RelationshipId = null, // There is no dedicated ID for "user in a specific guild"

            SupportPlanStatuses = guildUser.RoleIds.ToDictionary(roleId => roleId.ToString(), _ => new SupportStatusData
            {
                IsActive = true,

                // Discord role assignments never expire
                ExpiresAt = null,
                EnrolledAt = null
            }),
        };
    }
}