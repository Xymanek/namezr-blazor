using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;

namespace Namezr.Features.Notifications.Contracts;

internal interface INotificationEmailRenderer
{
    ValueTask<RenderedEmailNotification?> RenderIfSupportedAsync(Notification notification);
}

internal record RenderedEmailNotification
{
    public required string Subject { get; init; }
    public required string BodyHtml { get; init; }
    public required string BodyText { get; init; }
}

internal abstract class NotificationEmailRendererBase<TData> : INotificationEmailRenderer
{
    public async ValueTask<RenderedEmailNotification?> RenderIfSupportedAsync(Notification notification)
    {
        if (notification is not Notification<TData> typedNotification)
        {
            return null;
        }

        return await DoRenderAsync(typedNotification);
    }

    protected abstract ValueTask<RenderedEmailNotification> DoRenderAsync(Notification<TData> notification);
}

/// <summary>
/// Base class for component-based email notification renderers.
/// </summary>
/// <typeparam name="TData">The type of notification data</typeparam>
/// <typeparam name="TComponent">The component type that will render the email</typeparam>
[AutoConstructor]
internal abstract partial class NotificationEmailComponentRendererBase<TData, TComponent> : 
    NotificationEmailRendererBase<TData> where TComponent : IComponent
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILoggerFactory _loggerFactory;

    protected override async ValueTask<RenderedEmailNotification> DoRenderAsync(
        Notification<TData> notification)
    {
        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using HtmlRenderer htmlRenderer = new(scope.ServiceProvider, _loggerFactory);

        string html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            Dictionary<string, object?> dictionary = GetComponentParameters(notification);

            ParameterView parameters = ParameterView.FromDictionary(dictionary);
            HtmlRootComponent output =
                await htmlRenderer.RenderComponentAsync<TComponent>(parameters);

            return output.ToHtmlString();
        });

        return new RenderedEmailNotification
        {
            Subject = GetSubject(notification),
            BodyHtml = html,
            BodyText = GetPlainTextBody(notification)
        };
    }

    /// <summary>
    /// Gets the parameters to pass to the component.
    /// </summary>
    /// <param name="notification">The notification data</param>
    /// <returns>A dictionary of parameters</returns>
    protected abstract Dictionary<string, object?> GetComponentParameters(Notification<TData> notification);

    /// <summary>
    /// Gets the subject line for the email.
    /// </summary>
    /// <param name="notification">The notification data</param>
    /// <returns>The email subject</returns>
    protected abstract string GetSubject(Notification<TData> notification);

    /// <summary>
    /// Gets the plain text version of the email body.
    /// </summary>
    /// <param name="notification">The notification data</param>
    /// <returns>The plain text email body</returns>
    protected abstract string GetPlainTextBody(Notification<TData> notification);
}