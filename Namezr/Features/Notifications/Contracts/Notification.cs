namespace Namezr.Features.Notifications.Contracts;

internal abstract record Notification
{
    public required NotificationRecipient Recipient { get; init; }
}

internal class NotificationRecipient
{
    /// <summary>
    /// Gets the unique identifier of the creator associated with the notification.
    /// </summary>
    /// <remarks>
    /// The <c>CreatorId</c> property represents the optional GUID of the entity
    /// that created or initiated the notification.
    /// </remarks>
    public Guid? CreatorId { get; init; }

    /// <summary>
    /// If true, the notification will be sent to all staff members of <see cref="CreatorId"/>.
    /// Requires <see cref="CreatorId"/> to be set.
    /// </summary>
    public bool AllStaff { get; init; }

    public Guid? UserId { get; init; }
    public Guid? ConsumerId { get; init; }
}

/// <typeparam name="TData">
/// Must be
///
/// <list type="bullet">
/// <item>Immutable</item>
/// <item>JSON-serializable</item>
/// </list>
/// </typeparam>
internal record Notification<TData> : Notification
{
    public required TData Data { get; init; }
}