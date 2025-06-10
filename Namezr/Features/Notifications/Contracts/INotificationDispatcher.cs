using Namezr.Features.Notifications.Models;

namespace Namezr.Features.Notifications.Contracts;

public interface INotificationDispatcher
{
    /// <summary>
    /// Enqueues the notification for dispatching.
    /// Handles failures internally.
    /// </summary>
    /// <remarks>
    /// The sending logic for notifications will be processed in the background.
    /// </remarks>
    void Dispatch(INotification notification);
}