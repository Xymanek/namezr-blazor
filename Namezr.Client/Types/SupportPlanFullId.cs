using System.Text.Json.Serialization;

namespace Namezr.Client.Types;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
public record SupportPlanFullId
{
    public required Guid SupportTargetId { get; init; }
    public required string SupportPlanId { get; init; }
}