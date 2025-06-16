using MailKitSimplified.Sender.Abstractions;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;
using Namezr.Features.Notifications.Contracts;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Notifications.Services;

internal interface INotificationSenderEmail
{
    Task<bool> SendIfSupported(Notification notification);
}

[RegisterSingleton]
[AutoConstructor]
internal partial class NotificationSenderEmail : INotificationSenderEmail
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IEnumerable<INotificationEmailRenderer> _renderers;
    private readonly ISmtpSender _smtpSender;

    public async Task<bool> SendIfSupported(Notification notification)
    {
        if (notification.Recipient.UserId == null) return false;

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ApplicationUser user = await dbContext.Users
            .SingleAsync(user => user.Id == notification.Recipient.UserId);

        RenderedEmailNotification? rendered = await MaybeRender(notification);
        if (rendered == null) return false;

        await _smtpSender.WriteEmail
            .To(user.UserName, user.Email)
            .Subject(rendered.Subject)
            .BodyHtml(rendered.BodyHtml)
            .BodyText(rendered.BodyText)
            .SendAsync();

        return true;
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