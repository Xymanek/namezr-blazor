using Discord;
using Namezr.Client.Shared;
using Namezr.Features.Notifications.Contracts;

namespace Namezr.Features.Questionnaires.Notifications;

public record SubmissionStaffActionUserNotificationData
{
    public required Guid CreatorId { get; init; }
    public required string CreatorDisplayName { get; init; }
    public required Guid QuestionnaireId { get; init; }
    public required string QuestionnaireName { get; init; }

    /// <summary>
    /// ID of the user who submitted the submission.
    /// And thus will be sent the notification
    /// </summary>
    public required Guid SubmitterId { get; init; }
    public required string SubmitterName { get; init; }

    public required Guid SubmissionId { get; init; }
    public required int SubmissionNumber { get; init; }
    public required string SubmissionPublicUrl { get; init; }

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

    public static implicit operator Notification<SubmissionStaffActionUserNotificationData>(
        SubmissionStaffActionUserNotificationData data
    )
    {
        return data.ToNotification();
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

[AutoConstructor]
[RegisterSingleton(typeof(INotificationEmailRenderer))]
internal partial class SubmissionStaffActionUserNotificationEmailRenderer :
    NotificationEmailComponentRendererBase<SubmissionStaffActionUserNotificationData,
        SubmissionStaffActionUserEmailNotification>
{
    protected override string GetSubject(Notification<SubmissionStaffActionUserNotificationData> notification)
    {
        return GetActionSubject(notification.Data.Type);
    }

    protected override Dictionary<string, object?> GetComponentParameters(
        Notification<SubmissionStaffActionUserNotificationData> notification
    )
    {
        return new Dictionary<string, object?>
        {
            [nameof(SubmissionStaffActionUserEmailNotification.NotificationData)] = notification.Data,
        };
    }

    protected override string GetPlainTextBody(Notification<SubmissionStaffActionUserNotificationData> notification)
    {
        var data = notification.Data;
        var actionDescription = GetActionDescription(data.Type);
        var body = $"{GetActionSubject(data.Type)}\n\n{actionDescription}\n\n";

        if (data.Type == SubmissionStaffActionType.CommentAdded && !string.IsNullOrEmpty(data.CommentBody))
        {
            body += $"Comment: {data.CommentBody}\n\n";
        }

        if (data.Type is SubmissionStaffActionType.LabelAdded or SubmissionStaffActionType.LabelRemoved &&
            data.Label != null)
        {
            body += $"Label: {data.Label.Text}\n\n";
        }

        body += $"Submission: #{data.SubmissionNumber}\n";
        body += $"Submitter: {data.SubmitterName}\n";
        body += $"Questionnaire: {data.QuestionnaireName}\n";
        body += $"Creator: {data.CreatorDisplayName}\n\n";
        body += $"View the submission at: {data.SubmissionPublicUrl}";
        return body;
    }

    private string GetActionSubject(SubmissionStaffActionType type)
    {
        return type switch
        {
            SubmissionStaffActionType.ApprovalGranted => "Your Submission Has Been Approved",
            SubmissionStaffActionType.ApprovalRemoved => "Approval Removed From Your Submission",
            SubmissionStaffActionType.LabelAdded => "Label Added To Your Submission",
            SubmissionStaffActionType.LabelRemoved => "Label Removed From Your Submission",
            SubmissionStaffActionType.CommentAdded => "New Staff Comment On Your Submission",
            _ => "Staff Action On Your Submission"
        };
    }

    private string GetActionDescription(SubmissionStaffActionType type)
    {
        return type switch
        {
            SubmissionStaffActionType.ApprovalGranted => "Your submission has been approved by a staff member.",
            SubmissionStaffActionType.ApprovalRemoved =>
                "The approval on your submission has been removed by a staff member.",
            SubmissionStaffActionType.LabelAdded => "A staff member has added a label to your submission.",
            SubmissionStaffActionType.LabelRemoved => "A staff member has removed a label from your submission.",
            SubmissionStaffActionType.CommentAdded => "A staff member has added a comment to your submission.",
            _ => "A staff member has taken action on your submission."
        };
    }
}

[RegisterSingleton(typeof(INotificationDiscordRenderer))]
internal class SubmissionStaffActionUserNotificationDiscordRenderer
    : NotificationDiscordRendererBase<SubmissionStaffActionUserNotificationData>
{
    protected override ValueTask<RenderedDiscordNotification> DoRenderAsync(
        Notification<SubmissionStaffActionUserNotificationData> notification
    )
    {
        var data = notification.Data;
        var actionType = data.Type;

        var embedBuilder = new EmbedBuilder()
            .WithTitle(GetActionTitle(actionType))
            .WithDescription(GetActionDescription(actionType))
            .WithColor(GetActionColor(actionType))
            .WithTimestamp(DateTimeOffset.UtcNow)
            .WithUrl(data.SubmissionPublicUrl)
            .AddField("Submitter", data.SubmitterName)
            .AddField("Questionnaire", data.QuestionnaireName)
            .AddField("Creator", data.CreatorDisplayName);

        if (actionType == SubmissionStaffActionType.CommentAdded && !string.IsNullOrEmpty(data.CommentBody))
        {
            embedBuilder.AddField("Comment", data.CommentBody);
        }

        if (actionType is SubmissionStaffActionType.LabelAdded or SubmissionStaffActionType.LabelRemoved &&
            data.Label != null)
        {
            embedBuilder.AddField("Label", data.Label.Text);
        }

        embedBuilder.AddField("Submission #", data.SubmissionNumber.ToString());

        Embed embed = embedBuilder.Build();

        return ValueTask.FromResult(new RenderedDiscordNotification
        {
            Text = GetActionTitle(actionType),
            RichEmbed = embed,
            Embeds = [embed]
        });
    }

    private string GetActionTitle(SubmissionStaffActionType type)
    {
        return type switch
        {
            SubmissionStaffActionType.ApprovalGranted => "Your Submission Has Been Approved",
            SubmissionStaffActionType.ApprovalRemoved => "Approval Removed From Your Submission",
            SubmissionStaffActionType.LabelAdded => "Label Added To Your Submission",
            SubmissionStaffActionType.LabelRemoved => "Label Removed From Your Submission",
            SubmissionStaffActionType.CommentAdded => "New Staff Comment On Your Submission",
            _ => "Staff Action On Your Submission"
        };
    }

    private string GetActionDescription(SubmissionStaffActionType type)
    {
        return type switch
        {
            SubmissionStaffActionType.ApprovalGranted => "Your submission has been approved by a staff member.",
            SubmissionStaffActionType.ApprovalRemoved =>
                "The approval on your submission has been removed by a staff member.",
            SubmissionStaffActionType.LabelAdded => "A staff member has added a label to your submission.",
            SubmissionStaffActionType.LabelRemoved => "A staff member has removed a label from your submission.",
            SubmissionStaffActionType.CommentAdded => "A staff member has added a comment to your submission.",
            _ => "A staff member has taken action on your submission."
        };
    }

    private Color GetActionColor(SubmissionStaffActionType type)
    {
        return type switch
        {
            SubmissionStaffActionType.ApprovalGranted => Color.Green,
            SubmissionStaffActionType.ApprovalRemoved => Color.Red,
            SubmissionStaffActionType.LabelAdded => Color.Blue,
            SubmissionStaffActionType.LabelRemoved => Color.Orange,
            SubmissionStaffActionType.CommentAdded => Color.Purple,
            _ => Color.LightGrey
        };
    }
}