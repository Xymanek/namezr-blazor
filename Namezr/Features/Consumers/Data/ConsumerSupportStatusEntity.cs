using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Creators.Data;
using NodaTime;

namespace Namezr.Features.Consumers.Data;

[EntityTypeConfiguration(typeof(ConsumerSupportStatusEntityConfiguration))]
public class ConsumerSupportStatusEntity
{
    public TargetConsumerEntity Consumer { get; set; } = null!;
    public Guid ConsumerId { get; set; }

    [MaxLength(SupportPlanInfoEntity.SupportPlanIdMaxLength)]
    public required string SupportPlanId { get; set; }

    public required bool IsActive { get; set; }

    public Instant? EnrolledAt { get; set; }
    public Instant? ExpiresAt { get; set; }
}

internal class ConsumerSupportStatusEntityConfiguration : IEntityTypeConfiguration<ConsumerSupportStatusEntity>
{
    public void Configure(EntityTypeBuilder<ConsumerSupportStatusEntity> builder)
    {
        builder.HasKey(x => new { x.ConsumerId, x.SupportPlanId });
    }
}