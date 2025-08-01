﻿using Discord;
using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Questionnaires.Notifications;

public record SubmitterUpdatedValuesNotificationData
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
    public required string SubmissionStudioUrl { get; init; }

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
    NotificationEmailComponentRendererBase<SubmitterUpdatedValuesNotificationData,
        SubmitterUpdatedValuesEmailNotification>
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
               + $"Submission: #{notification.Data.SubmissionNumber}\n"
               + $"Submitter: {notification.Data.SubmitterName}\n"
               + $"Questionnaire: {notification.Data.QuestionnaireName}\n"
               + $"Creator: {notification.Data.CreatorDisplayName}\n\n"
               + $"View the submission at: {notification.Data.SubmissionStudioUrl}";
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
            .WithDescription($"A submitter has updated their submission values.\n\n"
                + $"Submission: #{data.SubmissionNumber}\n"
                + $"Submitter: {data.SubmitterName}\n"
                + $"Questionnaire: {data.QuestionnaireName}\n"
                + $"Creator: {data.CreatorDisplayName}")
            .WithColor(Color.Blue)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .AddField("Submission #", data.SubmissionNumber.ToString())
            .AddField("Submitter", data.SubmitterName)
            .AddField("Questionnaire", data.QuestionnaireName)
            .AddField("Creator", data.CreatorDisplayName)
            .WithUrl(data.SubmissionStudioUrl)
            .Build();

        return ValueTask.FromResult(new RenderedDiscordNotification
        {
            Text = $"Submission values updated for submission #{data.SubmissionNumber} by {data.SubmitterName}",
            RichEmbed = embed,
            Embeds = [embed]
        });
    }
}