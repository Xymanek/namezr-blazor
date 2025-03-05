using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.SelectionSeries.Data;

[EntityTypeConfiguration(typeof(SelectionCandidateEntityConfiguration))]
public abstract class SelectionCandidateEntity
{
    public Guid Id { get; set; }
}

internal class SelectionCandidateEntityConfiguration : IEntityTypeConfiguration<SelectionCandidateEntity>
{
    public void Configure(EntityTypeBuilder<SelectionCandidateEntity> builder)
    {
        builder.UseTpcMappingStrategy();
    }
}