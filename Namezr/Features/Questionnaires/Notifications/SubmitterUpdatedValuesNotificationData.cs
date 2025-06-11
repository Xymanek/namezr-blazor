using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Questionnaires.Notifications;

internal record SubmitterUpdatedValuesNotificationData
{
    public required Guid CreatorId { get; init; }
    public required Guid QuestionnaireId { get; init; }

    /// <summary>
    /// ID of the user who submitted the submission.
    /// </summary>
    public required Guid SubmitterId { get; init; }

    public required Guid SubmissionId { get; init; }
    public required string SubmissionUrl { get; init; }

    public Notification<SubmitterUpdatedValuesNotificationData> ToNotification()
    {
        return new Notification<SubmitterUpdatedValuesNotificationData>
        {
            Recipient = new NotificationRecipient
            {
                CreatorId = CreatorId,
                AllStaff = true,
            },

            Data = this,
        };
    }

    public static implicit operator Notification<SubmitterUpdatedValuesNotificationData>(
        SubmitterUpdatedValuesNotificationData data
    )
    {
        return data.ToNotification();
    }
}

[RegisterSingleton(typeof(INotificationEmailRenderer))]
internal class SubmitterUpdatedValuesNotificationDataEmailRenderer :
    NotificationEmailRendererBase<SubmitterUpdatedValuesNotificationData>
{
    protected override ValueTask<RenderedEmailNotification> DoRenderAsync(
        Notification<SubmitterUpdatedValuesNotificationData> notification
    )
    {
        throw new NotImplementedException();
    }
}