using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.Creators.Data;

// Support plan = a way to support creators with defined costs and/or benefits.
// Some support plans are fully hardcoded (e.g. twitch subscriptions) and won't have records in this table
// Others are dynamic (e.g. patreon campaigns) and will be stored here
[EntityTypeConfiguration(typeof(SupportPlanInfoEntityConfiguration))]
public class SupportPlanInfoEntity
{
    public SupportTargetEntity SupportTarget { get; set; } = null!;
    public Guid SupportTargetId { get; set; }

    [MaxLength(SupportPlanIdMaxLength)]
    public required string SupportPlanId { get; set; }

    [MaxLength(DisplayNameMaxLength)]
    public string? DisplayName { get; set; }
    
    public const int SupportPlanIdMaxLength = 100;
    public const int DisplayNameMaxLength = 100;
}

internal class SupportPlanInfoEntityConfiguration : IEntityTypeConfiguration<SupportPlanInfoEntity>
{
    public void Configure(EntityTypeBuilder<SupportPlanInfoEntity> builder)
    {
        builder.HasKey(x => new { x.SupportTargetId, x.SupportPlanId });
    }
}