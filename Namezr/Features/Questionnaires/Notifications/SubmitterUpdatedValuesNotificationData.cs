using Discord;
using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Questionnaires.Notifications;

public record SubmitterUpdatedValuesNotificationData
{
    public required Guid CreatorId { get; init; }
    public required Guid QuestionnaireId { get; init; }

    /// <summary>
    /// ID of the user who submitted the submission.
    /// </summary>
    public required Guid SubmitterId { get; init; }

    public required Guid SubmissionId { get; init; }
    public required int SubmissionNumber { get; init; }
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

[AutoConstructor]
[RegisterSingleton(typeof(INotificationEmailRenderer))]
internal partial class SubmitterUpdatedValuesNotificationDataEmailRenderer :
    NotificationEmailComponentRendererBase<SubmitterUpdatedValuesNotificationData, SubmitterUpdatedValuesEmailNotification>
{
    protected override string GetSubject(Notification<SubmitterUpdatedValuesNotificationData> notification)
    {
        return "Submission Values Updated";
    }

    protected override Dictionary<string, object?> GetComponentParameters(
        Notification<SubmitterUpdatedValuesNotificationData> notification
    )
    {
        return new Dictionary<string, object?>
        {
            [nameof(SubmitterUpdatedValuesEmailNotification.NotificationData)] = notification.Data,
        };
    }

    protected override string GetPlainTextBody(Notification<SubmitterUpdatedValuesNotificationData> notification)
    {
        return $"Submission Values Updated\n\n"
               + $"A submitter has updated their submission values.\n\n"
               + $"Submission: #{notification.Data.SubmissionNumber}\n\n"
               + $"View the submission at: {notification.Data.SubmissionUrl}";
    }
}

[RegisterSingleton(typeof(INotificationDiscordRenderer))]
internal class SubmitterUpdatedValuesNotificationDataDiscordRenderer
    : NotificationDiscordRendererBase<SubmitterUpdatedValuesNotificationData>
{
    protected override ValueTask<RenderedDiscordNotification> DoRenderAsync(
        Notification<SubmitterUpdatedValuesNotificationData> notification
    )
    {
        var data = notification.Data;

        Embed embed = new EmbedBuilder()
            .WithTitle("Submission Values Updated")
            .WithDescription("A submitter has updated their submission values.")
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("Submission #", data.SubmissionNumber.ToString())
            .WithUrl(data.SubmissionUrl)
            .Build();

        return ValueTask.FromResult(new RenderedDiscordNotification
        {
            Text = $"Submission values updated for submission #{data.SubmissionNumber}",
            RichEmbed = embed,
            Embeds = [embed]
        });
    }
}