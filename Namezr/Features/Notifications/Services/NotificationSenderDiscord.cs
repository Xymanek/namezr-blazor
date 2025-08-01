﻿using AspNet.Security.OAuth.Discord;
using Discord;
using Discord.Rest;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Notifications.Contracts;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.Discord;

namespace Namezr.Features.Notifications.Services;

internal interface INotificationSenderDiscord
{
    /// <returns>
    /// False if the user has disallowed DMs and no notification channel is configured.
    /// </returns>
    Task<bool> SendIfSupported(Notification notification);
}

[AutoConstructor]
[RegisterSingleton]
internal partial class NotificationSenderDiscord : INotificationSenderDiscord
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IEnumerable<INotificationDiscordRenderer> _renderers;
    private readonly IDiscordApiProvider _apiProvider;

    public async Task<bool> SendIfSupported(Notification notification)
    {
        ulong[] discordIds = await GetUserDiscordIds(notification);
        if (discordIds.Length == 0) return false;

        RenderedDiscordNotification? rendered = await MaybeRender(notification);
        if (rendered == null) return false;

        await using DiscordRestClient client = await _apiProvider.GetDiscordApiForApp();

        foreach (ulong discordId in discordIds)
        {
            // TODO: error handling per each
            RestUser user = await client.GetUserAsync(discordId);
            await user.SendMessageAsync(rendered.Text, embed: rendered.RichEmbed, embeds: rendered.Embeds);
        }

        // TODO: if failed due to privacy settings, fallback to creator-defined broadcast/fallback channel

        // TODO: return false if none of the IDs succeeded.
        return true;
    }

    private async Task<ulong[]> GetUserDiscordIds(Notification notification)
    {
        if (notification.Recipient.UserId == null) return [];

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        string[] userDiscordIds = await dbContext.UserLogins
            .Where(login =>
                login.UserId == notification.Recipient.UserId &&
                login.LoginProvider == DiscordAuthenticationDefaults.AuthenticationScheme
            )
            .Select(login => login.ProviderKey)
            .Distinct()
            .ToArrayAsync();

        return userDiscordIds.Select(ulong.Parse).ToArray();
    }

    private async Task<RenderedDiscordNotification?> MaybeRender(Notification notification)
    {
        foreach (INotificationDiscordRenderer renderer in _renderers)
        {
            RenderedDiscordNotification? rendered = await renderer.RenderDirectMessageIfSupportedAsync(notification);

            if (rendered != null)
            {
                return rendered;
            }
        }

        return null;
    }
}