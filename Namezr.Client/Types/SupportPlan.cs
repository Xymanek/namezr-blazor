namespace Namezr.Client.Types;

public record SupportPlan
{
    public required SupportPlanFullId Id { get; init; }

    public required SupportServiceType ServiceType { get; init; }
    public required string DisplayName { get; set; }
}