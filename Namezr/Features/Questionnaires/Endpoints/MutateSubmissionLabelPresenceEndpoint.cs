using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Behaviors] // Clear out the validation
[MapPost(ApiEndpointPaths.SubmissionLabelsPresenceMutate)]
internal partial class MutateSubmissionLabelPresenceEndpoint
{
    private static async ValueTask HandleAsync(
        MutateLabelPresenceRequest request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        ISubmissionAuditService submissionAudit,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .AsTracking()
            .AsSplitQuery()
            .Include(submission => submission.Labels)
            .Include(submission => submission.Version.Questionnaire)
            .SingleOrDefaultAsync(submission => submission.Id == request.SubmissionId, ct);

        if (submission == null) throw new Exception("Bad submission ID");
        await ValidateAccess();

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
            dbContext.SubmissionHistoryEntries.Add(submissionAudit.LabelAddedStaff(submission, label));
        }
        else
        {
            submission.Labels!.Remove(label);
            dbContext.SubmissionHistoryEntries.Add(submissionAudit.LabelRemovedStaff(submission, label));
        }

        await dbContext.SaveChangesAsync(ct);
        return;

        async Task ValidateAccess()
        {
            Guid userId = userAccessor.GetRequiredUserId(httpContextAccessor.HttpContext!);

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