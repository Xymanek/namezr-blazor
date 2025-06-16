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

// TODO: extract plumbing to base class
// This should contain only 3 methods:
// 1) Get parameters for the component
// 2) Get the plain text version of the email
// 3) Get the subject
// The component should be passed to the base class as a type argument
[AutoConstructor]
[RegisterSingleton(typeof(INotificationEmailRenderer))]
internal partial class SubmitterLeftCommentNotificationDataEmailRenderer :
    NotificationEmailRendererBase<SubmitterLeftCommentNotificationData>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;

    protected override async ValueTask<RenderedEmailNotification> DoRenderAsync(
        Notification<SubmitterLeftCommentNotificationData> notification
    )
    {
        await using HtmlRenderer htmlRenderer = new(_serviceProvider, _loggerFactory);

        string html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            Dictionary<string, object?> dictionary = new()
            {
                [nameof(SubmitterLeftCommentEmailNotification.NotificationData)] = notification.Data,
            };

            ParameterView parameters = ParameterView.FromDictionary(dictionary);
            HtmlRootComponent output =
                await htmlRenderer.RenderComponentAsync<SubmitterLeftCommentEmailNotification>(parameters);

            return output.ToHtmlString();
        });

        // Create plain text version from HTML
        string plainText = $"New Comment on Submission\n\n"
                           + $"A submitter has left a new comment on a questionnaire submission.\n\n"
                           + $"Comment: {notification.Data.CommentBody}\n\n"
                           + $"Creator: {notification.Data.CreatorDisplayName}\n"
                           + $"Questionnaire: {notification.Data.QuestionnaireName}\n"
                           + $"Submitter: {notification.Data.SubmitterName}\n"
                           + $"Submission: #{notification.Data.SubmissionNumber}\n\n"
                           + $"View the submission at: {notification.Data.SubmissionUrl}";

        return new RenderedEmailNotification
        {
            Subject = "New Comment on Submission",
            BodyHtml = html,
            BodyText = plainText
        };
    }
}