namespace Namezr.Features.Notifications.Contracts;

internal interface INotificationDispatcher
{
    /// <summary>
    /// Enqueues the notification for dispatching.
    /// Handles failures internally.
    /// </summary>
    /// <remarks>
    /// The sending logic for notifications will be processed in the background.
    /// </remarks>
    void Dispatch(Notification notification);
}