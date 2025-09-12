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

    /// <summary>
    /// Filter submissions by attribute key-value pairs. Dictionary keys are attribute keys, values are the required attribute values.
    /// Only submissions that have attributes matching ALL specified key-value pairs will be included.
    /// </summary>
    public Dictionary<string, string> RequiredAttributes { get; set; } = new();

    /// <summary>
    /// Filter out submissions that have any of these attribute key-value pairs.
    /// Dictionary keys are attribute keys, values are the attribute values to exclude.
    /// </summary>
    public Dictionary<string, string> ExcludedAttributes { get; set; } = new();
}