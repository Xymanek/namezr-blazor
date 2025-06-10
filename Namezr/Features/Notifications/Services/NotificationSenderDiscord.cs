using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Notifications.Services;

internal interface INotificationSenderDiscord
{
    /// <returns>
    /// False if the user has disallowed DMs and no notification channel is configured.
    /// </returns>
    Task<bool> SendIfSupported(Notification notification);
}

[RegisterSingleton]
internal class NotificationSenderDiscord : INotificationSenderDiscord
{
    public async Task<bool> SendIfSupported(Notification notification)
    {
        // throw new NotImplementedException();
        return false; // TODO: implement
    }
}