using System.Collections.Immutable;
using Namezr.Client.Types;

namespace Namezr.Features.Eligibility.Services;

/// <remarks>
/// Keep this object small for efficient caching
/// </remarks>
public record EligibilityResult
{
    public static readonly EligibilityResult None = new()
    {
        EligiblePlanIds = ImmutableHashSet<EligibilityPlanId>.Empty,
        Modifier = 0,
    };

    public required ImmutableHashSet<EligibilityPlanId> EligiblePlanIds { get; init; }
    public decimal Modifier { get; init; }
    public int MaxSubmissionsPerUser { get; init; } = 1;

    public bool Any => EligiblePlanIds.Count > 0;
};