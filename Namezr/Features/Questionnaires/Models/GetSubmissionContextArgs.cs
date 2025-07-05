using Namezr.Features.Identity.Data;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Models;

/// <summary>
/// Arguments for the GetSubmissionContextAsync method.
/// </summary>
internal record GetSubmissionContextArgs
{
    /// <summary>
    /// The questionnaire version entity (from stage 1).
    /// </summary>
    public required QuestionnaireVersionEntity QuestionnaireVersion { get; init; }

    /// <summary>
    /// The current user, or null if not logged in (from stage 2).
    /// </summary>
    public ApplicationUser? CurrentUser { get; init; }

    /// <summary>
    /// The mode determining if user is creating new, editing existing, or automatic mode.
    /// </summary>
    public required SubmissionMode SubmissionMode { get; init; }
    
    /// <summary>
    /// The ID of the submission that the user intends to view or update.
    /// Only relevant when <see cref="SubmissionMode"/> is <see cref="Models.SubmissionMode.EditExisting"/> 
    /// </summary>
    public Guid? SubmissionForUpdateId { get; init; }
}
