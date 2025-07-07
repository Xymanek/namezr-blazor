using Namezr.Features.Eligibility.Services;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Models;

public enum SubmissionMode
{
    /// <summary>
    /// Automatically determine mode with the following heuristic:
    ///
    /// <list type="number">
    /// <item>
    /// Edit <see cref="P:Namezr.Features.Questionnaires.Models.GetSubmissionContextArgs.SubmissionForUpdateId"/>
    /// if set
    /// </item>
    /// <item>
    /// Edit the first existing submission if available
    /// </item>
    /// <item>
    /// Create new
    /// </item>
    /// </list> 
    /// </summary>
    Automatic,

    /// <summary>
    /// Creating a new submission
    /// </summary>
    CreateNew,

    /// <summary>
    /// Editing an existing submission
    /// </summary>
    EditExisting,
}

public enum SubmissionDisabledReason
{
    SubmissionsClosed,
    NotLoggedIn,
    NotEligible,
    AlreadyApproved,
    SubmissionLimitReached,

    InvalidExistingSubmissionId,
}

public class QuestionnaireSubmissionContext
{
    // public required QuestionnaireVersionEntity QuestionnaireVersion { get; init; }
    // public required ApplicationUser? CurrentUser { get; init; }

    public required List<QuestionnaireSubmissionEntity> ExistingSubmissions { get; init; }
    public required EligibilityResult? EligibilityResult { get; init; }

    /// <summary>
    /// If null, means that the user can create/update the <see cref="SubmissionForUpdate"/>
    /// </summary>
    public required SubmissionDisabledReason? DisabledReason { get; init; }

    /// <remarks>
    /// False when the user is not logged in
    /// </remarks>
    public required bool CanCreateMoreSubmissions { get; init; }

    /// <summary>
    /// If null, implies that we are creating a new submission.
    /// </summary>
    /// <remarks>
    /// Not connected to any DB context and can be attached.
    /// </remarks>
    public required QuestionnaireSubmissionEntity? SubmissionForUpdate { get; init; }
}