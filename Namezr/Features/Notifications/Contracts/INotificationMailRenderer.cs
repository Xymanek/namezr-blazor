namespace Namezr.Features.Notifications.Contracts;

internal interface INotificationMailRenderer<in TNotification>
{
    ValueTask<RenderedEmailNotification> RenderAsync(TNotification notification);
}

internal record RenderedEmailNotification
{
    public required string Subject { get; init; }
    public required string BodyHtml { get; init; }
    public required string BodyText { get; init; }
};