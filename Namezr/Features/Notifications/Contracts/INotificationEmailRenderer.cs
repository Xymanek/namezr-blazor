namespace Namezr.Features.Notifications.Contracts;

internal interface INotificationEmailRenderer
{
    ValueTask<RenderedEmailNotification?> RenderIfSupportedAsync(Notification notification);
}

internal record RenderedEmailNotification
{
    public required string Subject { get; init; }
    public required string BodyHtml { get; init; }
    public required string BodyText { get; init; }
}

internal abstract class NotificationEmailRendererBase<TData> : INotificationEmailRenderer
{
    public async ValueTask<RenderedEmailNotification?> RenderIfSupportedAsync(Notification notification)
    {
        if (notification is not Notification<TData> typedNotification)
        {
            return null;
        }
        
        return await DoRenderAsync(typedNotification);
    }
    
    protected abstract ValueTask<RenderedEmailNotification> DoRenderAsync(Notification<TData> notification);
}