using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Notifications.Services;

internal interface INotificationSenderEmail
{
    Task<bool> SendIfSupported(Notification notification);
}

[RegisterSingleton]
[AutoConstructor]
internal partial class NotificationSenderEmail : INotificationSenderEmail
{
    private readonly IEnumerable<INotificationEmailRenderer> _renderers;

    public async Task<bool> SendIfSupported(Notification notification)
    {
        RenderedEmailNotification? rendered = await MaybeRender(notification);
        if (rendered == null) return false;

        throw new NotImplementedException();
        // return true;
    }

    private async Task<RenderedEmailNotification?> MaybeRender(Notification notification)
    {
        foreach (INotificationEmailRenderer renderer in _renderers)
        {
            return await renderer.RenderIfSupportedAsync(notification);
        }

        return null;
    }
}