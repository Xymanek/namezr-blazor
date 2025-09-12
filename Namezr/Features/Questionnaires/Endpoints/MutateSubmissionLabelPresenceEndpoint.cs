using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Shared;
using Namezr.Client.Studio.Questionnaires;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;
using Namezr.Features.Questionnaires.Notifications;
using Namezr.Features.Notifications.Contracts;
using Namezr.Infrastructure.Auth;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[AutoConstructor]
[MapPost(ApiEndpointPaths.SubmissionLabelsPresenceMutate)]
internal sealed partial class MutateSubmissionLabelPresenceEndpoint
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISubmissionAuditService _submissionAudit;
    private readonly INotificationDispatcher _notificationDispatcher;

    private async ValueTask HandleAsync(
        MutateLabelPresenceRequest request,
        CancellationToken ct
    )
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext!;
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .AsTracking()
            .AsSplitQuery()
            .Include(submission => submission.Labels)
            .Include(submission => submission.Version.Questionnaire.Creator)
            .Include(submission => submission.User)
            .SingleOrDefaultAsync(submission => submission.Id == request.SubmissionId, ct);

        if (submission == null) throw new Exception("Bad submission ID");

        SubmissionLabelEntity? label = await dbContext.SubmissionLabels
            .AsTracking()
            .SingleOrDefaultAsync(
                label =>
                    label.Id == request.LabelId &&
                    label.CreatorId == submission.Version.Questionnaire.CreatorId,
                ct
            );

        // TODO: 400 these
        if (label == null) throw new Exception("Bad label ID");
        if (label.CreatorId != submission.Version.Questionnaire.CreatorId)
        {
            throw new Exception("Label does not belong to same creator");
        }

        if (request.NewPresent)
        {
            submission.Labels!.Add(label);
            dbContext.SubmissionHistoryEntries.Add(_submissionAudit.LabelAddedStaff(submission, label));
        }
        else
        {
            submission.Labels!.Remove(label);
            dbContext.SubmissionHistoryEntries.Add(_submissionAudit.LabelRemovedStaff(submission, label));
        }

        // Trigger notification to submitter about staff label action, only if label is visible to submitter
        if (label.IsSubmitterVisible)
        {
            dbContext.OnSavedChangesOnce((_, _) =>
            {
                _notificationDispatcher.Dispatch(new SubmissionStaffActionUserNotificationData
                {
                    CreatorId = submission.Version.Questionnaire.CreatorId,
                    CreatorDisplayName = submission.Version.Questionnaire.Creator.DisplayName,
                    QuestionnaireId = submission.Version.Questionnaire.Id,
                    QuestionnaireName = submission.Version.Questionnaire.Title,
                    SubmitterId = submission.UserId,
                    SubmissionId = submission.Id,
                    SubmissionNumber = submission.Number,

                    SubmissionPublicUrl = UriHelper.BuildAbsolute(
                        httpContext.Request.Scheme,
                        httpContext.Request.Host,
                        httpContext.Request.PathBase,
                        $"/questionnaires/{submission.Version.Questionnaire.Id.NoHyphens()}/{submission.Id.NoHyphens()}"
                    ),

                    Type = request.NewPresent
                        ? SubmissionStaffActionType.LabelAdded
                        : SubmissionStaffActionType.LabelRemoved,
                    Label = new SubmissionLabelModel
                    {
                        Id = label.Id,
                        Text = label.Text,
                        Description = label.Description,
                        Colour = label.Colour,
                        IsSubmitterVisible = label.IsSubmitterVisible
                    }
                });
            });
        }

        await dbContext.SaveChangesAsync(ct);
    }
}
