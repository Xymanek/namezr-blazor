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
    protected override async ValueTask<Dictionary<string, SupportStatusData>> QueryStatuses(
        TargetConsumerEntity targetConsumer
    )
    {
        await using DiscordRestClient discordClient = await _discordApiProvider.GetDiscordApiForApp();

        RestGuildUser guildUser = await discordClient.GetGuildUserAsync(
            guildId: ulong.Parse(targetConsumer.SupportTarget.ServiceId),
            id: ulong.Parse(targetConsumer.ServiceId)
        );

        return guildUser.RoleIds.ToDictionary(roleId => roleId.ToString(), _ => new SupportStatusData
        {
            IsActive = true,

            // Discord role assignments never expire
            ExpiresAt = null,
            EnrolledAt = null
        });
    }

    protected override bool AllConsumersQuerySupported => false;

    protected override ValueTask<Dictionary<string, Dictionary<string, SupportStatusData>>>
        QueryAllConsumersStatuses(
            SupportTargetEntity supportTarget
        )
    {
        // TODO: can be supported
        throw new NotSupportedException();
    }
}