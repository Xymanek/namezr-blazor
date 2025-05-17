using Namezr.Client.Types;

namespace Namezr.Client.Studio.Questionnaires.Submissions;

public class SubmissionFilterModel
{
    public decimal? MinWeight { get; set; }
    public decimal? MaxWeight { get; set; }

    public List<EligibilityPlanId> IncludedPlanIds { get; set; } = [];
    public List<EligibilityPlanId> ExcludedPlanIds { get; set; } = [];

    public List<Guid> IncludedLabelIds { get; set; } = [];
    public List<Guid> ExcludedLabelIds { get; set; } = [];

    /// <summary>
    /// Null if filter not set. Otherwise value here = value on submission
    /// </summary>
    public bool? MatchIsApproved { get; set; }
}