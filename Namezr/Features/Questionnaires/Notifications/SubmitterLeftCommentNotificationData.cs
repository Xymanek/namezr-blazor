using Discord;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Questionnaires.Notifications;

public record SubmitterLeftCommentNotificationData
{
    public required Guid CreatorId { get; init; }
    public required string CreatorDisplayName { get; init; }

    public required Guid QuestionnaireId { get; init; }
    public required string QuestionnaireName { get; init; }

    /// <summary>
    /// ID of the user who submitted the submission.
    /// </summary>
    public required Guid SubmitterId { get; init; }

    public required string SubmitterName { get; init; }

    public required Guid SubmissionId { get; init; }
    public required int SubmissionNumber { get; init; }
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

[AutoConstructor]
[RegisterSingleton(typeof(INotificationEmailRenderer))]
internal partial class SubmitterLeftCommentNotificationDataEmailRenderer :
    NotificationEmailComponentRendererBase<SubmitterLeftCommentNotificationData, SubmitterLeftCommentEmailNotification>
{
    protected override string GetSubject(Notification<SubmitterLeftCommentNotificationData> notification)
    {
        return "New Comment on Submission";
    }

    protected override Dictionary<string, object?> GetComponentParameters(
        Notification<SubmitterLeftCommentNotificationData> notification
    )
    {
        return new Dictionary<string, object?>
        {
            [nameof(SubmitterLeftCommentEmailNotification.NotificationData)] = notification.Data,
        };
    }

    protected override string GetPlainTextBody(Notification<SubmitterLeftCommentNotificationData> notification)
    {
        return $"New Comment on Submission\n\n"
               + $"A submitter has left a new comment on a questionnaire submission.\n\n"
               + $"Comment: {notification.Data.CommentBody}\n\n"
               + $"Creator: {notification.Data.CreatorDisplayName}\n"
               + $"Questionnaire: {notification.Data.QuestionnaireName}\n"
               + $"Submitter: {notification.Data.SubmitterName}\n"
               + $"Submission: #{notification.Data.SubmissionNumber}\n\n"
               + $"View the submission at: {notification.Data.SubmissionUrl}";
    }
}

[RegisterSingleton(typeof(INotificationDiscordRenderer))]
internal class SubmitterLeftCommentNotificationDataDiscordRenderer
    : NotificationDiscordRendererBase<SubmitterLeftCommentNotificationData>
{
    protected override ValueTask<RenderedDiscordNotification> DoRenderAsync(
        Notification<SubmitterLeftCommentNotificationData> notification
    )
    {
        SubmitterLeftCommentNotificationData data = notification.Data;

        Embed embed = new EmbedBuilder()
            .WithTitle("New Comment on Submission")
            .WithDescription("A submitter has left a new comment on a questionnaire submission.")
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("Comment", data.CommentBody)
            .AddField("Creator", data.CreatorDisplayName)
            .AddField("Questionnaire", data.QuestionnaireName)
            .AddField("Submitter", data.SubmitterName)
            .AddField("Submission #", data.SubmissionNumber.ToString())
            .WithUrl(data.SubmissionUrl)
            .Build();

        return ValueTask.FromResult(new RenderedDiscordNotification
        {
            Text = $"New comment from {data.SubmitterName} on submission #{data.SubmissionNumber}",
            RichEmbed = embed,
            Embeds = [embed]
        });
    }
}