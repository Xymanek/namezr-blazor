namespace Namezr.Client.Types;

public record SupportPlan
{
    public required SupportServiceType ServiceType { get; init; }
    public required Guid SupportTargetId { get; set; }

    public required string SupportPlanId { get; set; }
    public required string DisplayName { get; set; }
}