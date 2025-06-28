using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit; // Add this
using Namezr.Features.Consumers.Services;
using Namezr.Features.Eligibility.Services;
using Namezr.Features.Identity.Data;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Models;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Services;

internal class QuestionnaireAccessService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly IEligibilityService _eligibilityService;

    public QuestionnaireAccessService(
        ApplicationDbContext dbContext,
        IdentityUserAccessor userAccessor,
        IEligibilityService eligibilityService
    )
    {
        _dbContext = dbContext;
        _userAccessor = userAccessor;
        _eligibilityService = eligibilityService;
    }

    public async Task<QuestionnaireAccessResult> GetQuestionnaireAccessResult(
        Guid questionnaireId,
        HttpContext httpContext,
        CancellationToken ct = default
    )
    {
        QuestionnaireVersionEntity? versionEntity = await _dbContext.QuestionnaireVersions
            .AsSplitQuery()
            .AsNoTracking()
            .Include(x => x.Questionnaire.EligibilityConfiguration).ThenInclude(x => x.Options)
            .Include(x => x.Questionnaire.Creator.SupportTargets)
            .Include(x => x.Fields!).ThenInclude(x => x.Field)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(q => q.Questionnaire.Id == questionnaireId, ct);

        if (versionEntity is null)
        {
            return new QuestionnaireAccessResult { DisabledReason = DisabledReason.NotFound };
        }

        if (versionEntity.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.Closed)
        {
            return new QuestionnaireAccessResult { DisabledReason = DisabledReason.SubmissionsClosed };
        }

        ApplicationUser? user = await _userAccessor.GetUserAsync(httpContext);

        if (user is null)
        {
            return new QuestionnaireAccessResult { DisabledReason = DisabledReason.NotLoggedIn };
        }

        EligibilityResult eligibilityResult = await _eligibilityService.ClassifyEligibility(
            user.Id,
            versionEntity.Questionnaire.EligibilityConfiguration,
            UserStatusSyncEagerness.Default
        );

        if (!eligibilityResult.Any)
        {
            return new QuestionnaireAccessResult { DisabledReason = DisabledReason.NotEligible };
        }

        QuestionnaireSubmissionEntity? existingSubmission = await _dbContext.QuestionnaireSubmissions
            .AsSplitQuery()
            .Include(x => x.FieldValues)
            .Include(x => x.History!.Where(entry => entry is SubmissionHistoryPublicCommentEntity))
            .Include(x => x.Labels!.Where(label => label.IsSubmitterVisible))
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Version.QuestionnaireId == questionnaireId, ct);

        if (existingSubmission is null)
        {
            if (versionEntity.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.EditExistingOnly)
            {
                return new QuestionnaireAccessResult { DisabledReason = DisabledReason.SubmissionsClosed };
            }
        }
        else
        {
            if (
                existingSubmission.ApprovedAt is not null &&
                versionEntity.Questionnaire.ApprovalMode == QuestionnaireApprovalMode.RequireApprovalProhibitEditingApproved
            )
            {
                return new QuestionnaireAccessResult { DisabledReason = DisabledReason.AlreadyApproved };
            }
        }

        return new QuestionnaireAccessResult
        {
            QuestionnaireVersion = versionEntity,
            CurrentUser = user,
            EligibilityResult = eligibilityResult,
            ExistingSubmission = existingSubmission,
            DisabledReason = null,
        };
    }
}