using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Creators.Data;
using NodaTime;

namespace Namezr.Features.Consumers.Data;

// This is potentially speculative, e.g. we will create a TargetConsumerEntity for a
// twitch user for a specific channel, even if the user has never visited the channel before
public class TargetConsumerEntity
{
    public Guid Id { get; set; }
    
    public SupportTargetEntity SupportTarget { get; set; } = null!;
    public Guid SupportTargetId { get; set; }

    /// <summary>
    /// The global ID of the user on the service.
    /// </summary>
    [MaxLength(250)]
    public required string ServiceUserId { get; set; }

    /// <summary>
    /// <para>
    /// The ID of the link between the user and the support target.
    /// E.g. Patreon membership ID.
    /// </para>
    /// <para>
    /// May be <see langword="null"/> as not all services have such concept
    /// </para>
    /// </summary>
    [MaxLength(250)]
    public string? RelationshipId { get; set; }
    
    /// <summary>
    /// The last time the consumer status was synced with the external service.
    /// </summary>
    public Instant? LastSyncedAt { get; set; }
    
    public ICollection<ConsumerSupportStatusEntity>? SupportStatuses { get; set; }
}

internal class ConsumerEntityConfiguration : IEntityTypeConfiguration<TargetConsumerEntity>
{
    public void Configure(EntityTypeBuilder<TargetConsumerEntity> builder)
    {
        // No duplicate records of same user per service.
        // Note as support targets are "per creator", this still
        // permits same user across multiple creators on same platform.
        builder.HasAlternateKey(x => new { x.SupportTargetId, ServiceId = x.ServiceUserId });
    }
}