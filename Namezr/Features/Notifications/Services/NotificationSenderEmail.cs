using Namezr.Features.Notifications.Contracts;
using Namezr.Features.Notifications.Models;

namespace Namezr.Features.Notifications.Services;

internal interface INotificationSenderEmail
{
    bool Supports(INotification notification);
    Task Send(INotification notification);
}

[RegisterSingleton]
[AutoConstructor]
internal partial class NotificationSenderEmail : INotificationSenderEmail
{
    private readonly IServiceProvider _services;
    
    public bool Supports(INotification notification)
    {
        throw new NotImplementedException();
    }

    public async Task Send(INotification notification)
    {
        _services.GetRequiredService(GetRendererType(notification));
        
        throw new NotImplementedException();
    }

    private static Type GetRendererType(INotification notification)
    {
        return typeof(INotificationMailRenderer<>).MakeGenericType(notification.GetType());
    }
}