using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Namezr.Client.Types;

// TODO: try to convert to subclasses (how does SSR -> WASM json conversion work?)
public record EligibilityPlan
{
    public EligibilityPlanId Id { get; }

    public EligibilityType Type { get; }

    public SupportPlan? SupportPlan { get; }
    public VirtualEligibilityType? VirtualEligibilityType { get; }

    public string? DefaultPriorityGroup { get; init; }

    public EligibilityPlan(SupportPlan supportPlan)
    {
        Type = EligibilityType.SupportPlan;
        SupportPlan = supportPlan;

        Id = new EligibilityPlanId(supportPlan.Id);
    }

    public EligibilityPlan(VirtualEligibilityType virtualEligibilityType)
    {
        Type = EligibilityType.Virtual;
        VirtualEligibilityType = virtualEligibilityType;

        Id = new EligibilityPlanId(virtualEligibilityType);

        DefaultPriorityGroup = "Other";
    }

    [JsonConstructor]
    private EligibilityPlan(
        EligibilityPlanId id,
        EligibilityType type,
        SupportPlan? supportPlan,
        VirtualEligibilityType? virtualEligibilityType
    )
    {
        if (
            type == EligibilityType.SupportPlan != supportPlan is not null ||
            type == EligibilityType.Virtual != virtualEligibilityType is not null
        )
        {
            throw new ArgumentException("Type and support plan/virtual eligibility type must match");
        }

        // TODO: recalculate/validate
        Id = id;

        Type = type;
        SupportPlan = supportPlan;
        VirtualEligibilityType = virtualEligibilityType;
    }
}

public record EligibilityPlanId
{
    public required EligibilityType Type { get; init; }
    public SupportPlanFullId? SupportPlanId { get; init; }
    public VirtualEligibilityType? VirtualEligibilityType { get; init; }

    public EligibilityPlanId()
    {
    }

    [SetsRequiredMembers]
    public EligibilityPlanId(SupportPlanFullId supportPlanId)
    {
        Type = EligibilityType.SupportPlan;
        SupportPlanId = supportPlanId;
    }

    [SetsRequiredMembers]
    public EligibilityPlanId(VirtualEligibilityType virtualEligibilityType)
    {
        Type = EligibilityType.Virtual;
        VirtualEligibilityType = virtualEligibilityType;
    }
}