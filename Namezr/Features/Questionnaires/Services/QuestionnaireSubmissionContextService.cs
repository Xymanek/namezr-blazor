using Namezr.Client.Studio.Questionnaires.Edit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Eligibility.Services;
using Namezr.Features.Identity.Data;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Models;
using Namezr.Features.Consumers.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Services;

/// <summary>
/// Service for retrieving questionnaire submission context in a staged approach.
/// <para>
/// <b>Staged approach:</b>
/// <list type="number">
///   <item>
///     <description>Call <see cref="GetLatestQuestionnaireVersionAsync"/> to retrieve the latest <see cref="QuestionnaireVersionEntity"/> for a questionnaire.</description>
///   </item>
///   <item>
///     <description>Fetch the <see cref="ApplicationUser"/> (if any) for the current request.</description>
///   </item>
///   <item>
///     <description>Pass the retrieved <see cref="QuestionnaireVersionEntity"/> and <see cref="ApplicationUser"/> to <see cref="GetSubmissionContextAsync"/> to get the full <see cref="QuestionnaireSubmissionContext"/>.</description>
///   </item>
/// </list>
/// This separation allows for more flexible and testable code, and enables callers to perform additional logic between stages if needed.
/// </para>
/// </summary>
public interface IQuestionnaireSubmissionContextService
{
    /// <summary>
    /// Retrieves the full submission context for a given questionnaire version and user.
    /// <para>Stage 3 of the staged approach. Requires a <see cref="QuestionnaireVersionEntity"/> and <see cref="ApplicationUser"/> (or null).</para>
    /// </summary>
    /// <param name="questionnaireVersion">The questionnaire version entity (from stage 1).</param>
    /// <param name="currentUser">The current user, or null if not logged in (from stage 2).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The submission context for the questionnaire version and user.</returns>
    Task<QuestionnaireSubmissionContext> GetSubmissionContextAsync(QuestionnaireVersionEntity questionnaireVersion, ApplicationUser? currentUser, CancellationToken ct);

    /// <summary>
    /// Retrieves the latest version entity for a questionnaire.
    /// <para>Stage 1 of the staged approach. Pass the result to <see cref="GetSubmissionContextAsync"/>.</para>
    /// </summary>
    /// <param name="questionnaireId">The questionnaire ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The latest questionnaire version entity, or null if not found.</returns>
    Task<QuestionnaireVersionEntity?> GetLatestQuestionnaireVersionAsync(Guid questionnaireId, CancellationToken ct);

    /// <summary>
    /// Retrieves a specific questionnaire version by ID.
    /// <para>Alternative to stage 1 of the staged approach. Pass the result to <see cref="GetSubmissionContextAsync"/>.</para>
    /// </summary>
    /// <param name="questionnaireVersionId">The questionnaire version ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The questionnaire version entity, or null if not found.</returns>
    Task<QuestionnaireVersionEntity?> GetQuestionnaireVersionAsync(Guid questionnaireVersionId, CancellationToken ct);
}

[AutoConstructor]
[RegisterScoped]
internal partial class QuestionnaireSubmissionContextService : IQuestionnaireSubmissionContextService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly IEligibilityService _eligibilityService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Important: GetQuestionnaireVersionAsync and GetLatestQuestionnaireVersionAsync must stay in sync with each other.
    public async Task<QuestionnaireVersionEntity?> GetQuestionnaireVersionAsync(Guid questionnaireVersionId, CancellationToken ct)
    {
        return await _dbContext.QuestionnaireVersions
            .AsSplitQuery()
            .AsNoTracking()
            .Include(x => x.Questionnaire.EligibilityConfiguration).ThenInclude(x => x.Options)
            .Include(x => x.Questionnaire.Creator.SupportTargets)
            .Include(x => x.Fields!).ThenInclude(x => x.Field)
            .FirstOrDefaultAsync(q => q.Id == questionnaireVersionId, ct);
    }

    public async Task<QuestionnaireVersionEntity?> GetLatestQuestionnaireVersionAsync(Guid questionnaireId, CancellationToken ct)
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

    public async Task<QuestionnaireSubmissionContext> GetSubmissionContextAsync(QuestionnaireVersionEntity questionnaireVersion, ApplicationUser? currentUser, CancellationToken ct)
    {
        if (questionnaireVersion is null)
        {
            // TODO: return a result object instead of throwing
            throw new Exception($"QuestionnaireVersion cannot be null.");
        }

        SubmissionDisabledReason? disabledReason = null;
        if (questionnaireVersion.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.Closed)
        {
            disabledReason = SubmissionDisabledReason.SubmissionsClosed;
        }

        EligibilityResult? eligibilityResult = null;
        if (currentUser is null)
        {
            disabledReason ??= SubmissionDisabledReason.NotLoggedIn;
        }
        else
        {
            eligibilityResult = await _eligibilityService.ClassifyEligibility(
                currentUser.Id,
                questionnaireVersion.Questionnaire.EligibilityConfiguration,
                UserStatusSyncEagerness.Default
            );

            if (!eligibilityResult.Any)
            {
                disabledReason ??= SubmissionDisabledReason.NotEligible;
            }
        }

        QuestionnaireSubmissionEntity? existingSubmission = null;
        if (currentUser is not null)
        {
            existingSubmission = await _dbContext.QuestionnaireSubmissions
                .AsSplitQuery()
                .Include(x => x.FieldValues)
                .FirstOrDefaultAsync(x => x.UserId == currentUser.Id && x.Version.QuestionnaireId == questionnaireVersion.Questionnaire.Id, ct);

            if (existingSubmission is null)
            {
                if (questionnaireVersion.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.EditExistingOnly)
                {
                    disabledReason ??= SubmissionDisabledReason.SubmissionsClosed;
                }
            }
            else
            {
                if (
                    existingSubmission.ApprovedAt is not null &&
                    questionnaireVersion.Questionnaire.ApprovalMode == QuestionnaireApprovalMode.RequireApprovalProhibitEditingApproved
                )
                {
                    disabledReason ??= SubmissionDisabledReason.AlreadyApproved;
                }
            }
        }

        return new QuestionnaireSubmissionContext
        {
            QuestionnaireVersion = questionnaireVersion,
            CurrentUser = currentUser,
            ExistingSubmission = existingSubmission,
            EligibilityResult = eligibilityResult,
            DisabledReason = disabledReason,
        };
    }
}