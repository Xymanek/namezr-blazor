using Namezr.Features.Eligibility.Services;
using Namezr.Features.Identity.Data;
using Namezr.Features.Questionnaires.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Models;

public class QuestionnaireAccessResult
{
    public QuestionnaireVersionEntity? QuestionnaireVersion { get; init; }
    public ApplicationUser? CurrentUser { get; init; }
    public EligibilityResult? EligibilityResult { get; init; }
    public QuestionnaireSubmissionEntity? ExistingSubmission { get; init; }
    public DisabledReason? DisabledReason { get; init; }

    public bool IsSuccess => DisabledReason is null && QuestionnaireVersion is not null;
}

public enum DisabledReason
{
    SubmissionsClosed,
    NotLoggedIn,
    NotEligible,
    AlreadyApproved,
    NotFound,
}