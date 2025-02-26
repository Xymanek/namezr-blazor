using Namezr.Client.Types;
using NodaTime;

namespace Namezr.Features.Consumers.Services;

public record UserSupportStatusEntry
{
    public required Guid CreatorId { get; init; }
    public required Guid SupportTargetId { get; init; }
    public required SupportServiceType SupportServiceType { get; init; }
    public required string SupportTargetServiceId { get; init; }
    public required string SupportPlanId { get; init; }

    public required Guid UserId { get; init; }
    public required Guid ConsumerId { get; init; }
    public required string ConsumerServiceId { get; init; }

    public required SupportStatusData Data { get; init; }
    public required Instant LastSyncedAt { get; init; }
}

public record struct SupportStatusData
{
    public required bool IsActive { get; init; }
    public required Instant? ExpiresAt { get; init; }
    public required Instant? EnrolledAt { get; init; }
}
