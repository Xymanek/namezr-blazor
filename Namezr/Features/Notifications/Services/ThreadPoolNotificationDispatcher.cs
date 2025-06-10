using Namezr.Features.Notifications.Contracts;
using Namezr.Features.Notifications.Models;

namespace Namezr.Features.Notifications.Services;

/// <summary>
/// Thread-pool based dispatcher.
/// Does not persist notifications across application restarts.
/// Will hold application stop to attempt to send all queued notifications.
/// </summary>
[AutoConstructor]
internal partial class ThreadPoolNotificationDispatcher : INotificationDispatcher, IHostedService
{
    private readonly ILogger<ThreadPoolNotificationDispatcher> _logger;
    private readonly INotificationRouter _notificationRouter;

    private bool IsStopping
    {
        get => Volatile.Read(ref field);
        set => Volatile.Write(ref field, value);
    }

    public void Dispatch(INotification notification)
    {
        if (IsStopping)
        {
            _logger.LogError(
                "Notification dispatching failed: application is (being) stopped. Lost notification: {notification}",
                notification
            );
            return;
        }

        Interlocked.Increment(ref _pendingNotifications);
        Task.Run(async () =>
        {
            try
            {
                await _notificationRouter.Route(notification);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Notification routing failed: {notification}", notification);
            }
            finally
            {
                Interlocked.Decrement(ref _pendingNotifications);
                _notificationCompletedEvent.Set();
            }
        });
    }

    /// <summary>
    /// Notifications that have been dispatched but not yet completed.
    /// </summary>
    private long _pendingNotifications;

    private readonly AutoResetEvent _notificationCompletedEvent = new(false);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        IsStopping = true;

        // Spin on a background thread until all notifications are completed.
        return Task.Run(() =>
        {
            while (Volatile.Read(ref _pendingNotifications) > 0 && !cancellationToken.IsCancellationRequested)
            {
                _notificationCompletedEvent.WaitOne(TimeSpan.FromSeconds(0.5));
            }
        }, cancellationToken);
    }
}