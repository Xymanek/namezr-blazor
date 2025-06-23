using Discord;

namespace Namezr.Features.Notifications.Contracts;

internal interface INotificationDiscordRenderer
{
    ValueTask<RenderedDiscordNotification?> RenderDirectMessageIfSupportedAsync(Notification notification);
}

internal record RenderedDiscordNotification
{
    public string? Text { get; init; }
    public required Embed RichEmbed { get; init; }
    public required Embed[] Embeds { get; init; }
}

internal abstract class NotificationDiscordRendererBase<TData> : INotificationDiscordRenderer
{
    public async ValueTask<RenderedDiscordNotification?> RenderDirectMessageIfSupportedAsync(Notification notification)
    {
        if (notification is not Notification<TData> typedNotification)
        {
            return null;
        }
        
        return await DoRenderAsync(typedNotification);
    }
    
    protected abstract ValueTask<RenderedDiscordNotification> DoRenderAsync(Notification<TData> notification);
}