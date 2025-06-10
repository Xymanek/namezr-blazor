using Namezr.Client.Shared;
using Namezr.Features.Notifications.Contracts;
using Namezr.Features.Notifications.Models;

namespace Namezr.Features.Questionnaires.Notifications;

internal record SubmissionStaffActionUserNotification : INotification
{
    Guid? INotification.CreatorId => CreatorId;
    bool INotification.ReceivingAllStaff => false;
    Guid? INotification.ReceivingUserId => SubmitterId;
    Guid? INotification.ReceivingConsumerId => null;

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
    public SubmissionLabelModel? Label { get; set; }

    /// <summary>
    /// The content of the comment when <see cref="Type"/> is <see cref="SubmissionStaffActionType.CommentAdded"/>.
    /// Null for other action types.
    /// </summary>
    public string? CommentBody { get; set; }
}

public enum SubmissionStaffActionType
{
    ApprovalGranted,
    ApprovalRemoved,

    LabelAdded,
    LabelRemoved,

    CommentAdded,
}

[RegisterSingleton]
internal class SubmissionStaffActionUserNotificationEmailRenderer :
    INotificationMailRenderer<SubmissionStaffActionUserNotification>
{
    public ValueTask<RenderedEmailNotification> RenderAsync(SubmissionStaffActionUserNotification notification)
    {
        throw new NotImplementedException();
    }
}