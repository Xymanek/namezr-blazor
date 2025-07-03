using Namezr.Features.Eligibility.Services;
using Namezr.Features.Identity.Data;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Models;

public enum SubmissionMode
{
    /// <summary>
    /// Automatically determine mode: edit first existing submission if available, otherwise create new
    /// </summary>
    Automatic,

    /// <summary>
    /// Creating a new submission
    /// </summary>
    CreateNew,

    /// <summary>
    /// Editing an existing submission
    /// </summary>
    EditExisting
}

public enum SubmissionDisabledReason
{
    SubmissionsClosed,
    NotLoggedIn,
    NotEligible,
    AlreadyApproved,
    SubmissionLimitReached,
}

public class QuestionnaireSubmissionContext
{
    public required QuestionnaireVersionEntity QuestionnaireVersion { get; init; }
    public required ApplicationUser? CurrentUser { get; init; }
    public required List<QuestionnaireSubmissionEntity> ExistingSubmissions { get; init; }
    public required EligibilityResult? EligibilityResult { get; init; }
    public required SubmissionDisabledReason? DisabledReason { get; init; }
}