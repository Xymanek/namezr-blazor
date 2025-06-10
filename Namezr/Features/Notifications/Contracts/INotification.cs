namespace Namezr.Features.Notifications.Models;

/// <summary>
/// Implementations must be
///
/// <list type="bullet">
/// <item>Immutable</item>
/// <item>JSON-serializable</item>
/// </list>
/// </summary>
public interface INotification
{
    /// <summary>
    /// Gets the unique identifier of the creator associated with the notification.
    /// </summary>
    /// <remarks>
    /// The <c>CreatorId</c> property represents the optional GUID of the entity
    /// that created or initiated the notification.
    /// </remarks>
    Guid? CreatorId { get; }

    /// <summary>
    /// If true, the notification will be sent to all staff members of <see cref="CreatorId"/>.
    /// Requires <see cref="CreatorId"/> to be set.
    /// </summary>
    bool ReceivingAllStaff { get; }

    Guid? ReceivingUserId { get; }
    Guid? ReceivingConsumerId { get; }
}