using System.Collections.Immutable;
using Namezr.Client.Types;

namespace Namezr.Features.Eligibility.Services;

public record EligibilityResult
{
    public static readonly EligibilityResult None = new()
    {
        EligiblePlanIds = ImmutableHashSet<EligibilityPlanId>.Empty,
        Modifier = 0,
    };

    public required ImmutableHashSet<EligibilityPlanId> EligiblePlanIds { get; init; }
    public decimal Modifier { get; init; }

    public bool Any => EligiblePlanIds.Count > 0;
};