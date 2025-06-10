using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Notifications.Contracts;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Notifications.Services;

internal interface INotificationRouter
{
    Task Route(Notification notification);
}

[RegisterSingleton]
[AutoConstructor]
internal partial class NotificationRouter : INotificationRouter
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly INotificationSenderDiscord _notificationSenderDiscord;
    private readonly INotificationSenderEmail _notificationSenderEmail;
    private readonly ILogger<NotificationRouter> _logger;

    public async Task Route(Notification notification)
    {
        if (await TrySplitAndRouteAllStaff(notification))
        {
            return;
        }

        // Definitely not AllStaff = true here
        await RouteDefinitelyIndividual(notification);
    }

    /// <summary>
    /// If to staff, re-create as per-staff-user notifications and send those
    /// </summary>
    /// <returns>True if per-staff-user notifications were sent</returns>
    private async Task<bool> TrySplitAndRouteAllStaff(Notification notification)
    {
        if (!notification.Recipient.AllStaff) return false;
        Guard.IsNotNull(notification.Recipient.CreatorId);

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        Guid[] staffUserIds = await dbContext.CreatorStaff
            .Where(staff => staff.CreatorId == notification.Recipient.CreatorId)
            .Select(staff => staff.UserId)
            .ToArrayAsync();

        foreach (Guid userId in staffUserIds)
        {
            // Note: this preserves the actual type with the data
            // TODO: some kind of "notification heritage" tracking?
            Notification staffNotification = notification with
            {
                Recipient = new NotificationRecipient
                {
                    CreatorId = notification.Recipient.CreatorId,
                    AllStaff = false,
                    UserId = userId,
                },
            };

            // TODO: error handling per notification
            // TODO: in parallel
            await Route(staffNotification);
        }

        return true;
    }

    private async Task RouteDefinitelyIndividual(Notification notification)
    {
        // TODO: fancy logic to determine whether to prefer discord or email.
        // Probably should be controlled by user account settings, creator settings or even both.

        // For now, just send via both methods.

        bool[] isSupportedResults = await Task.WhenAll(
            // Note: discord 1st in this list since it's likely faster to arrive/be noticed
            _notificationSenderDiscord.SendIfSupported(notification),
            _notificationSenderEmail.SendIfSupported(notification)
        );

        if (isSupportedResults.All(b => b == false))
        {
            _logger.LogError("Notification routing failed: no supported methods for {notification}", notification);
        }

        // TODO: store the notification to show in the UI as well
        // if (notification.Recipient.UserId != null)
        // {
        // }
    }
}