using CommunityToolkit.Diagnostics;
using Namezr.Client.Studio.Questionnaires.Edit;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Eligibility.Services;
using Namezr.Features.Identity.Data;
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
internal interface IQuestionnaireSubmissionContextService
{
    /// <summary>
    /// Retrieves the full submission context for a given questionnaire version and user.
    /// <para>Stage 3 of the staged approach. Requires a <see cref="QuestionnaireVersionEntity"/> and <see cref="ApplicationUser"/> (or null).</para>
    /// </summary>
    /// <param name="args">The arguments containing questionnaire version, current user, and submission mode.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The submission context for the questionnaire version and user.</returns>
    Task<QuestionnaireSubmissionContext> GetSubmissionContextAsync(GetSubmissionContextArgs args, CancellationToken ct);

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
    private readonly IEligibilityService _eligibilityService;

    // Important: GetQuestionnaireVersionAsync and GetLatestQuestionnaireVersionAsync must stay in sync with each other.
    public async Task<QuestionnaireVersionEntity?> GetQuestionnaireVersionAsync(
        Guid questionnaireVersionId,
        CancellationToken ct
    )
    {
        return await _dbContext.QuestionnaireVersions
            .AsSplitQuery()
            .AsNoTracking()
            .Include(x => x.Questionnaire.EligibilityConfiguration).ThenInclude(x => x.Options)
            .Include(x => x.Questionnaire.Creator.SupportTargets)
            .Include(x => x.Fields!).ThenInclude(x => x.Field)
            .FirstOrDefaultAsync(q => q.Id == questionnaireVersionId, ct);
    }

    public async Task<QuestionnaireVersionEntity?> GetLatestQuestionnaireVersionAsync(
        Guid questionnaireId,
        CancellationToken ct
    )
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

    public async Task<QuestionnaireSubmissionContext> GetSubmissionContextAsync(
        GetSubmissionContextArgs args,
        CancellationToken ct
    )
    {
        Guard.IsNotNull(args);

        SubmissionDisabledReason? disabledReason = null;
        if (args.QuestionnaireVersion.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.Closed)
        {
            disabledReason = SubmissionDisabledReason.SubmissionsClosed;
        }

        EligibilityResult? eligibilityResult = null;
        List<QuestionnaireSubmissionEntity> existingSubmissions = [];
        QuestionnaireSubmissionEntity? submissionForUpdate = null;
        bool canCreateMoreSubmissions = false;

        if (args.CurrentUser is null)
        {
            disabledReason ??= SubmissionDisabledReason.NotLoggedIn;
        }
        else
        {
            eligibilityResult = await _eligibilityService.ClassifyEligibility(
                args.CurrentUser.Id,
                args.QuestionnaireVersion.Questionnaire.EligibilityConfiguration,
                UserStatusSyncEagerness.Default
            );

            if (!eligibilityResult.Any)
            {
                disabledReason ??= SubmissionDisabledReason.NotEligible;
            }

            existingSubmissions = await _dbContext.QuestionnaireSubmissions
                .AsSplitQuery()
                .Include(x => x.FieldValues)
                .Where(x =>
                    x.UserId == args.CurrentUser.Id &&
                    x.Version.QuestionnaireId == args.QuestionnaireVersion.Questionnaire.Id
                )
                .OrderBy(s => s.SubmittedAt)
                .ToListAsync(ct);

            // Determine the submission that we are trying to view/update 
            submissionForUpdate = DetermineSubmissionForUpdate(
                args, existingSubmissions, ref disabledReason
            );

            // If editing is only allowed for existing, but user has none, disable
            if (!existingSubmissions.Any())
            {
                if (
                    args.QuestionnaireVersion.Questionnaire.SubmissionOpenMode ==
                    QuestionnaireSubmissionOpenMode.EditExistingOnly
                )
                {
                    disabledReason ??= SubmissionDisabledReason.SubmissionsClosed;
                }
            }
            else
            {
                // If the submission is approved and editing is prohibited, disable
                if (
                    submissionForUpdate is { ApprovedAt: not null } &&
                    args.QuestionnaireVersion.Questionnaire.ApprovalMode ==
                    QuestionnaireApprovalMode.RequireApprovalProhibitEditingApproved
                )
                {
                    disabledReason ??= SubmissionDisabledReason.AlreadyApproved;
                }
            }
            
            canCreateMoreSubmissions = existingSubmissions.Count < eligibilityResult.MaxSubmissionsPerUser;

            // Enforce submission limit only when creating new submissions
            if (
                submissionForUpdate == null &&
                !canCreateMoreSubmissions
            )
            {
                disabledReason ??= SubmissionDisabledReason.SubmissionLimitReached;
            }
        }

        return new QuestionnaireSubmissionContext
        {
            // QuestionnaireVersion = args.QuestionnaireVersion,
            // CurrentUser = args.CurrentUser,
            ExistingSubmissions = existingSubmissions,
            EligibilityResult = eligibilityResult,
            DisabledReason = disabledReason,
            CanCreateMoreSubmissions = canCreateMoreSubmissions,
            SubmissionForUpdate = submissionForUpdate,
        };
    }

    /// <summary>
    /// Determines the appropriate questionnaire submission to update or view based on the provided arguments and existing submissions.
    ///
    /// See <see cref="F:Namezr.Features.Questionnaires.Models.SubmissionMode.Automatic"/> for rules
    /// </summary>
    /// <param name="args">The arguments containing submission context details, such as submission mode and optional submission ID for update.</param>
    /// <param name="existingSubmissions">A list of existing questionnaire submissions for the user.</param>
    /// <param name="disabledReason">When applicable, provides the reason why submission editing or updating is disabled.</param>
    /// <returns>The selected questionnaire submission for update or view, or null if no suitable submission is found.</returns>
    private static QuestionnaireSubmissionEntity? DetermineSubmissionForUpdate(
        GetSubmissionContextArgs args,
        List<QuestionnaireSubmissionEntity> existingSubmissions,
        ref SubmissionDisabledReason? disabledReason
    )
    {
        return args.SubmissionMode switch
        {
            SubmissionMode.Automatic when args.SubmissionForUpdateId is not null
                => AttemptUseExistingSubmission(args, existingSubmissions, ref disabledReason),

            SubmissionMode.Automatic => existingSubmissions.FirstOrDefault(),
            SubmissionMode.EditExisting => AttemptUseExistingSubmission(args, existingSubmissions, ref disabledReason),

            _ => null,
        };
    }

    /// <summary>
    /// Attempts to find and return an existing questionnaire submission for update based on the specified arguments.
    /// </summary>
    /// <param name="args">The arguments containing details related to the questionnaire submission context, including the submission ID for update.</param>
    /// <param name="existingSubmissions">A list of existing questionnaire submissions.</param>
    /// <param name="disabledReason">
    /// A reference to a variable that indicates the reason the submission could not be selected for update, if applicable.
    /// This will be set to <see cref="SubmissionDisabledReason.InvalidExistingSubmissionId"/> if the submission ID is invalid.
    /// </param>
    /// <returns>The existing questionnaire submission entity for update if found; otherwise, null.</returns>
    private static QuestionnaireSubmissionEntity? AttemptUseExistingSubmission(
        GetSubmissionContextArgs args,
        List<QuestionnaireSubmissionEntity> existingSubmissions,
        ref SubmissionDisabledReason? disabledReason
    )
    {
        Guard.IsNotNull(args.SubmissionForUpdateId);

        QuestionnaireSubmissionEntity? submissionForUpdate = existingSubmissions
            .SingleOrDefault(x => x.Id == args.SubmissionForUpdateId);

        if (submissionForUpdate is null)
        {
            disabledReason ??= SubmissionDisabledReason.InvalidExistingSubmissionId;
        }

        return submissionForUpdate;
    }
}