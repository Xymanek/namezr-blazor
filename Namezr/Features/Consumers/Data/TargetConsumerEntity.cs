using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Creators.Data;

namespace Namezr.Features.Consumers.Data;

public class TargetConsumerEntity
{
    public Guid Id { get; set; }
    
    public SupportTargetEntity SupportTarget { get; set; } = null!;
    public Guid SupportTargetId { get; set; }

    public required string ServiceId { get; set; }

    public ICollection<ConsumerSupportStatusEntity>? SupportStatuses { get; set; }
}

internal class ConsumerEntityConfiguration : IEntityTypeConfiguration<TargetConsumerEntity>
{
    public void Configure(EntityTypeBuilder<TargetConsumerEntity> builder)
    {
        // No duplicate records of same user per service.
        // Note as support targets are "per creator", this still
        // permits same user across multiple creators on same platform.
        builder.HasAlternateKey(x => new { x.SupportTargetId, x.ServiceId });
    }
}