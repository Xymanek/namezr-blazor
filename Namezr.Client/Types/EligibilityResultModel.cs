﻿namespace Namezr.Client.Types;

public class EligibilityResultModel
{
    public required HashSet<EligibilityPlanId> EligiblePlanIds { get; init; }
    public decimal Modifier { get; init; }
    public int MaxSubmissionsPerUser { get; init; }

    public bool Any => EligiblePlanIds.Count > 0;
}