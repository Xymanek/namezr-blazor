namespace Namezr.Client.Types;

public record struct SupportPlanFullId
{
    public required Guid SupportTargetId { get; set; }
    public required string SupportPlanId { get; set; }
}