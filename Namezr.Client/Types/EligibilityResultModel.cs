namespace Namezr.Client.Types;

public class EligibilityResultModel
{
    public required IReadOnlySet<EligibilityPlanId> EligiblePlanIds { get; init; }
    public decimal Modifier { get; init; }

    public bool Any => EligiblePlanIds.Count > 0;
}