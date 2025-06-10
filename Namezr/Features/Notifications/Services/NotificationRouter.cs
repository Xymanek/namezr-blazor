using Microsoft.EntityFrameworkCore;
using Namezr.Features.Notifications.Models;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Notifications.Services;

internal interface INotificationRouter
{
    Task Route(INotification notification);
}

[RegisterSingleton]
[AutoConstructor]
internal partial class NotificationRouter : INotificationRouter
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly INotificationSenderDiscord _notificationSenderDiscord;
    private readonly INotificationSenderEmail _notificationSenderEmail;

    public async Task Route(INotification notification)
    {
    }
}