using Namezr.Features.Eligibility.Services;
using Namezr.Features.Identity.Data;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Models;

public enum SubmissionDisabledReason
{
    SubmissionsClosed,
    NotLoggedIn,
    NotEligible,
    AlreadyApproved,
}

public class QuestionnaireSubmissionContext
{
    public required QuestionnaireVersionEntity QuestionnaireVersion { get; init; }
    public required ApplicationUser? CurrentUser { get; init; }
    public required QuestionnaireSubmissionEntity? ExistingSubmission { get; init; }
    public required EligibilityResult? EligibilityResult { get; init; }
    public required SubmissionDisabledReason? DisabledReason { get; init; }
}