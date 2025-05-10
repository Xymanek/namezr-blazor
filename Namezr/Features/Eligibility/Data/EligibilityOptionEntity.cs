using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Studio.Eligibility.Edit;
using Namezr.Client.Types;

namespace Namezr.Features.Eligibility.Data;

[EntityTypeConfiguration(typeof(EligibilityOptionEntityConfiguration))]
public class EligibilityOptionEntity
{
    public long Id { get; set; }

    public EligibilityConfigurationEntity Configuration { get; set; } = null!;
    public long ConfigurationId { get; set; }

    public required EligibilityPlanId PlanId { get; set; }

    public required int Order { get; set; }

    [MaxLength(EligibilityOptionEditModel.PriorityGroupMaxLength)]
    public required string PriorityGroup { get; set; }

    public required decimal PriorityModifier { get; set; }
    
    // TODO: this is too confusing and does not allow "select everyone before this". Set a default value instead.
    /// <summary>
    /// Valid lower value candidates are always selected before candidates with a higher value.
    /// If <c>null</c>, assumed to be highest/latest.
    /// When multiple options match, the lowest value will be used.
    /// </summary>
    public required int? SelectionWave { get; set; }
}

internal class EligibilityOptionEntityConfiguration : IEntityTypeConfiguration<EligibilityOptionEntity>
{
    public void Configure(EntityTypeBuilder<EligibilityOptionEntity> builder)
    {
        builder.OwnsOne(x => x.PlanId, eligibilityBuilder =>
        {
            eligibilityBuilder.ToJson();

            eligibilityBuilder.OwnsOne(x => x.SupportPlanId);
        });
        
        // TODO: index
        
        // builder.HasIndex(x => new { x.ConfigurationId, x.PlanId })
        //     .IsUnique();
        
        // builder
        //     .HasIndex(x => new
        //     {
        //         x.ConfigurationId,
        //         x.PlanId.SupportPlanId,
        //         x.PlanId.VirtualEligibilityType,
        //     })
        //     .IsUnique();
    }
}