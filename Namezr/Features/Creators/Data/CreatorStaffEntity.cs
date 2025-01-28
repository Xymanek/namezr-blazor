using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Identity.Data;

namespace Namezr.Features.Creators.Data;

[EntityTypeConfiguration(typeof(CreatorStaffEntityConfiguration))]
public class CreatorStaffEntity
{
    public CreatorEntity Creator { get; set; } = null!;
    public Guid CreatorId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Guid UserId { get; set; }
    
    public ICollection<SupportTargetEntity>? OwnedSupportTargets { get; set; }
}

internal class CreatorStaffEntityConfiguration : IEntityTypeConfiguration<CreatorStaffEntity>
{
    public void Configure(EntityTypeBuilder<CreatorStaffEntity> builder)
    {
        builder.HasKey(x => new { x.CreatorId, x.UserId });
    }
}