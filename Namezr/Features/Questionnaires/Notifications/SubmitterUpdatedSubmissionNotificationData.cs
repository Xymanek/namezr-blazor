using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Questionnaires.Notifications;

internal record SubmitterUpdatedSubmissionNotificationData
{
    public required Guid CreatorId { get; init; }
    public required Guid QuestionnaireId { get; init; }

    /// <summary>
    /// ID of the user who submitted the submission.
    /// </summary>
    public required Guid SubmitterId { get; init; }

    public required Guid SubmissionId { get; init; }
    public required string SubmissionUrl { get; init; }
    
    public Notification<SubmitterUpdatedSubmissionNotificationData> ToNotification()
    {
        return new Notification<SubmitterUpdatedSubmissionNotificationData>
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
internal class SubmitterUpdatedSubmissionNotificationDataEmailRenderer :
    NotificationEmailRendererBase<SubmitterUpdatedSubmissionNotificationData>
{
    protected override ValueTask<RenderedEmailNotification> DoRenderAsync(
        Notification<SubmitterUpdatedSubmissionNotificationData> notification
    )
    {
        throw new NotImplementedException();
    }
}