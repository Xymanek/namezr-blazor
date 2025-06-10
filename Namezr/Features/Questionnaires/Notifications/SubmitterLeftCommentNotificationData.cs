using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Questionnaires.Notifications;

internal record SubmitterLeftCommentNotificationData
{
    public required Guid CreatorId { get; init; }
    public required Guid QuestionnaireId { get; init; }

    /// <summary>
    /// ID of the user who submitted the submission.
    /// </summary>
    public required Guid SubmitterId { get; init; }

    public required Guid SubmissionId { get; init; }
    public required string SubmissionUrl { get; init; }

    public required string CommentBody { get; init; }

    public Notification<SubmitterLeftCommentNotificationData> ToNotification()
    {
        return new Notification<SubmitterLeftCommentNotificationData>
        {
            Recipient = new NotificationRecipient
            {
                CreatorId = CreatorId,
                AllStaff = true,
            },

            Data = this,
        };
    }
}

[RegisterSingleton(typeof(INotificationEmailRenderer))]
internal class SubmitterLeftCommentNotificationDataEmailRenderer :
    NotificationEmailRendererBase<SubmitterLeftCommentNotificationData>
{
    protected override ValueTask<RenderedEmailNotification> DoRenderAsync(
        Notification<SubmitterLeftCommentNotificationData> notification
    )
    {
        throw new NotImplementedException();
    }
}