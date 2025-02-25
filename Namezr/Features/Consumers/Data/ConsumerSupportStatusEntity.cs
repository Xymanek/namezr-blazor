using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NodaTime;

namespace Namezr.Features.Consumers.Data;

[EntityTypeConfiguration(typeof(ConsumerSupportStatusEntityConfiguration))]
public class ConsumerSupportStatusEntity
{
    public TargetConsumerEntity Consumer { get; set; } = null!;
    public Guid ConsumerId { get; set; }

    public required string SupportPlanId { get; set; }

    public required bool IsActive { get; set; }

    public Instant? EnrolledAt { get; set; }
    public Instant? ExpiresAt { get; set; }

    public required Instant LastSyncedAt { get; set; }
}

internal class ConsumerSupportStatusEntityConfiguration : IEntityTypeConfiguration<ConsumerSupportStatusEntity>
{
    public void Configure(EntityTypeBuilder<ConsumerSupportStatusEntity> builder)
    {
        builder.HasKey(x => new { x.ConsumerId, x.SupportPlanId });
    }
}