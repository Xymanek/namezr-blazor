using Namezr.Client.Shared;
using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Questionnaires.Notifications;

internal record SubmissionStaffActionUserNotificationData
{
    public required Guid CreatorId { get; init; }
    public required Guid QuestionnaireId { get; init; }

    /// <summary>
    /// ID of the user who submitted the submission.
    /// And thus will be sent the notification
    /// </summary>
    public required Guid SubmitterId { get; init; }

    public required Guid SubmissionId { get; init; }
    public required string SubmissionUrl { get; init; }

    public required SubmissionStaffActionType Type { get; init; }

    /// <summary>
    /// Required if <see cref="Type"/> is <see cref="SubmissionStaffActionType.LabelAdded"/>
    /// or <see cref="SubmissionStaffActionType.LabelRemoved"/>.
    /// Null otherwise.
    /// </summary>
    // Not immutable, but I don't want to create a billion types to represent the same thing
    // (blazor form requires mutable)
    public SubmissionLabelModel? Label { get; init; }

    /// <summary>
    /// The content of the comment when <see cref="Type"/> is <see cref="SubmissionStaffActionType.CommentAdded"/>.
    /// Null for other action types.
    /// </summary>
    public string? CommentBody { get; init; }

    public Notification<SubmissionStaffActionUserNotificationData> ToNotification()
    {
        return new Notification<SubmissionStaffActionUserNotificationData>
        {
            Recipient = new NotificationRecipient
            {
                CreatorId = CreatorId,
                AllStaff = false,
                UserId = SubmitterId,
                ConsumerId = null,
            },
            
            Data = this,
        };
    }
}

public enum SubmissionStaffActionType
{
    ApprovalGranted,
    ApprovalRemoved,

    LabelAdded,
    LabelRemoved,

    CommentAdded,
}

[RegisterSingleton(typeof(INotificationEmailRenderer))]
internal class SubmissionStaffActionUserNotificationEmailRenderer :
    NotificationEmailRendererBase<SubmissionStaffActionUserNotificationData>
{
    protected override ValueTask<RenderedEmailNotification> DoRenderAsync(
        Notification<SubmissionStaffActionUserNotificationData> notification
    )
    {
        throw new NotImplementedException();
    }
}