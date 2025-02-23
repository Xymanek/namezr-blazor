using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Features.Eligibility.Data;

[EntityTypeConfiguration(typeof(EligibilityOptionEntityConfiguration))]
public class EligibilityOptionEntity
{
    public long Id { get; set; }
    
    public EligibilityConfigurationEntity Configuration { get; set; } = null!;
    public long ConfigurationId { get; set; }

    public required ImmutableList<string> IdChain { get; set; }

    public required int Order { get; set; }

    [MaxLength(EligibilityOptionEditModel.PriorityGroupMaxLength)]
    public required string PriorityGroup { get; set; }

    public required decimal PriorityModifier { get; set; }
}

internal class EligibilityOptionEntityConfiguration : IEntityTypeConfiguration<EligibilityOptionEntity>
{
    public void Configure(EntityTypeBuilder<EligibilityOptionEntity> builder)
    {
        builder.HasAlternateKey(x => new { x.ConfigurationId, x.IdChain });
    }
}