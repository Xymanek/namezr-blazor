using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Studio.Questionnaires.Edit;
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