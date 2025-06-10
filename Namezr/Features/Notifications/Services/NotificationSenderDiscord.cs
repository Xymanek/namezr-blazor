using Namezr.Features.Notifications.Models;

namespace Namezr.Features.Notifications.Services;

internal interface INotificationSenderDiscord
{
    bool Supports(INotification notification);

    /// <returns>
    /// False if the user has disallowed DMs and no notification channel is configured.
    /// </returns>
    Task<bool> Send(INotification notification);
}

internal class NotificationSenderDiscord : INotificationSenderDiscord
{
    public bool Supports(INotification notification)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> Send(INotification notification)
    {
        throw new NotImplementedException();
    }
}