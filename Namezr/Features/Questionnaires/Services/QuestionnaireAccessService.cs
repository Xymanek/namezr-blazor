using Microsoft.EntityFrameworkCore;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Consumers.Services;
using Namezr.Features.Eligibility.Data;
using Namezr.Features.Eligibility.Services;
using Namezr.Features.Identity.Data;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Services;

public interface IQuestionnaireAccessService
{
    /// <summary>
    /// Loads questionnaire version with all necessary related data for access validation
    /// </summary>
    Task<QuestionnaireVersionEntity?> LoadQuestionnaireVersionAsync(
        Guid questionnaireId, 
        CancellationToken ct = default);

    /// <summary>
    /// Loads questionnaire version by version ID with all necessary related data
    /// </summary>
    Task<QuestionnaireVersionEntity?> LoadQuestionnaireVersionByIdAsync(
        Guid versionId, 
        CancellationToken ct = default);

    /// <summary>
    /// Validates questionnaire access and returns detailed access information
    /// </summary>
    Task<QuestionnaireAccessResult> ValidateAccessAsync(
        QuestionnaireVersionEntity questionnaireVersion,
        ApplicationUser? user = null,
        CancellationToken ct = default);

    /// <summary>
    /// Loads existing submission for a user and questionnaire
    /// </summary>
    Task<QuestionnaireSubmissionEntity?> LoadExistingSubmissionAsync(
        Guid userId,
        Guid questionnaireId,
        CancellationToken ct = default);
}

public record QuestionnaireAccessResult
{
    public required bool IsAccessible { get; init; }
    public required QuestionnaireAccessDeniedReason? DeniedReason { get; init; }
    public EligibilityResult? EligibilityResult { get; init; }
    public QuestionnaireSubmissionEntity? ExistingSubmission { get; init; }
}

public enum QuestionnaireAccessDeniedReason
{
    QuestionnaireNotFound,
    SubmissionsClosed,
    NotLoggedIn,
    NotEligible,
    AlreadyApproved,
    EditExistingOnlyButNoSubmission
}

[AutoConstructor]
[RegisterScoped]
internal partial class QuestionnaireAccessService : IQuestionnaireAccessService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEligibilityService _eligibilityService;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<QuestionnaireVersionEntity?> LoadQuestionnaireVersionAsync(
        Guid questionnaireId, 
        CancellationToken ct = default)
    {
        return await _dbContext.QuestionnaireVersions
            .AsSplitQuery()
            .AsNoTracking()
            .Include(x => x.Questionnaire.EligibilityConfiguration).ThenInclude(x => x.Options)
            .Include(x => x.Questionnaire.Creator.SupportTargets)
            .Include(x => x.Fields!).ThenInclude(x => x.Field)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(q => q.Questionnaire.Id == questionnaireId, ct);
    }

    public async Task<QuestionnaireVersionEntity?> LoadQuestionnaireVersionByIdAsync(
        Guid versionId, 
        CancellationToken ct = default)
    {
        return await _dbContext.QuestionnaireVersions
            .AsNoTracking()
            .Include(x => x.Questionnaire.Creator)
            .Include(x => x.Fields!).ThenInclude(x => x.Field)
            .SingleOrDefaultAsync(x => x.Id == versionId, ct);
    }

    public async Task<QuestionnaireAccessResult> ValidateAccessAsync(
        QuestionnaireVersionEntity questionnaireVersion,
        ApplicationUser? user = null,
        CancellationToken ct = default)
    {
        // If user is not provided, try to get from current context
        user ??= await _userAccessor.GetUserAsync(_httpContextAccessor.HttpContext!);

        // Check if submissions are closed
        if (questionnaireVersion.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.Closed)
        {
            return new QuestionnaireAccessResult
            {
                IsAccessible = false,
                DeniedReason = QuestionnaireAccessDeniedReason.SubmissionsClosed
            };
        }

        // Check if user is logged in
        if (user is null)
        {
            return new QuestionnaireAccessResult
            {
                IsAccessible = false,
                DeniedReason = QuestionnaireAccessDeniedReason.NotLoggedIn
            };
        }

        // Check eligibility
        EligibilityResult eligibilityResult = await _eligibilityService.ClassifyEligibility(
            user.Id,
            questionnaireVersion.Questionnaire.EligibilityConfiguration,
            UserStatusSyncEagerness.Default
        );

        if (!eligibilityResult.Any)
        {
            return new QuestionnaireAccessResult
            {
                IsAccessible = false,
                DeniedReason = QuestionnaireAccessDeniedReason.NotEligible,
                EligibilityResult = eligibilityResult
            };
        }

        // Load existing submission
        QuestionnaireSubmissionEntity? existingSubmission = await LoadExistingSubmissionAsync(
            user.Id, 
            questionnaireVersion.QuestionnaireId, 
            ct);

        // Check submission mode restrictions
        if (existingSubmission is null)
        {
            if (questionnaireVersion.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.EditExistingOnly)
            {
                return new QuestionnaireAccessResult
                {
                    IsAccessible = false,
                    DeniedReason = QuestionnaireAccessDeniedReason.EditExistingOnlyButNoSubmission,
                    EligibilityResult = eligibilityResult
                };
            }
        }
        else
        {
            // Check if editing approved submissions is prohibited
            if (existingSubmission.ApprovedAt is not null &&
                questionnaireVersion.Questionnaire.ApprovalMode == QuestionnaireApprovalMode.RequireApprovalProhibitEditingApproved)
            {
                return new QuestionnaireAccessResult
                {
                    IsAccessible = false,
                    DeniedReason = QuestionnaireAccessDeniedReason.AlreadyApproved,
                    EligibilityResult = eligibilityResult,
                    ExistingSubmission = existingSubmission
                };
            }
        }

        // All checks passed
        return new QuestionnaireAccessResult
        {
            IsAccessible = true,
            DeniedReason = null,
            EligibilityResult = eligibilityResult,
            ExistingSubmission = existingSubmission
        };
    }

    public async Task<QuestionnaireSubmissionEntity?> LoadExistingSubmissionAsync(
        Guid userId,
        Guid questionnaireId,
        CancellationToken ct = default)
    {
        return await _dbContext.QuestionnaireSubmissions
            .AsSplitQuery()
            .Include(x => x.FieldValues)
            // If these filters are ever adjusted, other code will need to change
            .Include(x => x.History!.Where(entry => entry is SubmissionHistoryPublicCommentEntity))
            .Include(x => x.Labels!.Where(label => label.IsSubmitterVisible))
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Version.QuestionnaireId == questionnaireId, ct);
    }
}