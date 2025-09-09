using System.Diagnostics;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Notifications.Contracts;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Notifications;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Endpoints;

public record ToggleSubmissionApprovalRequest
{
    public required Guid SubmissionId { get; init; }
    public required bool ShouldApprove { get; init; }
}

public record ToggleSubmissionApprovalResponse
{
    public required bool IsApproved { get; init; }
}

[Handler]
[Behaviors] // Clear out the validation
[MapPost(ApiEndpointPaths.SubmissionApprovalToggle)]
internal partial class ToggleSubmissionApprovalEndpoint
{
    private static async ValueTask<ToggleSubmissionApprovalResponse> HandleAsync(
        ToggleSubmissionApprovalRequest request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        IClock clock,
        ISubmissionAuditService auditService,
        INotificationDispatcher notificationDispatcher,
        CancellationToken ct)
    {
        HttpContext httpContext = httpContextAccessor.HttpContext!;
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .AsTracking()
            .Include(x => x.Version.Questionnaire.Creator)
            .SingleOrDefaultAsync(x => x.Id == request.SubmissionId, ct);

        if (submission == null)
        {
            throw new Exception("Bad submission ID");
        }

        await ValidateAccess();

        if (request.ShouldApprove)
        {
            submission.ApprovedAt = clock.GetCurrentInstant();
            submission.ApproverId = (await userAccessor.GetRequiredUserAsync(httpContext)).Id;

            dbContext.SubmissionHistoryEntries.Add(auditService.ApprovalGrant(submission));

            // Dispatch notification for approval granted
            notificationDispatcher.Dispatch(new SubmissionStaffActionUserNotificationData
            {
                CreatorId = submission.Version.Questionnaire.CreatorId,
                CreatorDisplayName = submission.Version.Questionnaire.Creator.DisplayName,
                QuestionnaireId = submission.Version.QuestionnaireId,
                QuestionnaireName = submission.Version.Questionnaire.Title,
                SubmitterId = submission.UserId,
                SubmissionId = submission.Id,
                SubmissionNumber = submission.Number,

                SubmissionPublicUrl = UriHelper.BuildAbsolute(
                    httpContext.Request.Scheme,
                    httpContext.Request.Host,
                    httpContext.Request.PathBase,
                    $"/questionnaires/{submission.Version.QuestionnaireId.NoHyphens()}/{submission.Id.NoHyphens()}"
                ),

                Type = SubmissionStaffActionType.ApprovalGranted
            });
        }
        else
        {
            submission.ApprovedAt = null;
            submission.ApproverId = null;

            dbContext.SubmissionHistoryEntries.Add(auditService.ApprovalRemoval(submission));

            // Dispatch notification for approval removed
            notificationDispatcher.Dispatch(new SubmissionStaffActionUserNotificationData
            {
                CreatorId = submission.Version.Questionnaire.CreatorId,
                CreatorDisplayName = submission.Version.Questionnaire.Creator.DisplayName,
                QuestionnaireId = submission.Version.QuestionnaireId,
                QuestionnaireName = submission.Version.Questionnaire.Title,
                SubmitterId = submission.UserId,
                SubmissionId = submission.Id,
                SubmissionNumber = submission.Number,
                SubmissionPublicUrl = UriHelper.BuildAbsolute(
                    httpContext.Request.Scheme,
                    httpContext.Request.Host,
                    httpContext.Request.PathBase,
                    $"/questionnaires/{submission.Version.QuestionnaireId.NoHyphens()}/{submission.Id.NoHyphens()}"
                ),
                Type = SubmissionStaffActionType.ApprovalRemoved
            });
        }

        await dbContext.SaveChangesAsync(ct);

        return new ToggleSubmissionApprovalResponse
        {
            IsApproved = submission.ApprovedAt is not null
        };

        async Task ValidateAccess()
        {
            Guid userId = userAccessor.GetRequiredUserId(httpContext);

            // ReSharper disable once AccessToDisposedClosure
            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(staff =>
                    staff.UserId == userId &&
                    staff.CreatorId == submission.Version.Questionnaire.CreatorId
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}