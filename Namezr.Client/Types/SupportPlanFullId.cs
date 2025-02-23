using System.Text.Json.Serialization;

namespace Namezr.Client.Types;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
public record struct SupportPlanFullId
{
    // TODO: figure out how to make these work with blazor SSR -> WASM deserialization
    public /*required*/ Guid SupportTargetId { get; set; }
    public /*required*/ string? SupportPlanId { get; set; }
}