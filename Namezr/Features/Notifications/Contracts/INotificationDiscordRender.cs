using Discord;

namespace Namezr.Features.Notifications.Contracts;

internal interface INotificationDiscordRender
{
    ValueTask<RenderedDiscordNotification?> RenderDirectMessageIfSupportedAsync(Notification notification);
}

internal record RenderedDiscordNotification
{
    public string? Text { get; init; }
    public required Embed RichEmbed { get; init; }
    public required Embed[] Embeds { get; init; }
}