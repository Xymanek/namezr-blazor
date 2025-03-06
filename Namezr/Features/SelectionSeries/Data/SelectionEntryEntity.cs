using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Namezr.Features.SelectionSeries.Data;

[EntityTypeConfiguration(typeof(SelectionEntryEntityConfiguration))]
public abstract class SelectionEntryEntity
{
    public long Id { get; set; }

    public SelectionBatchEntity Batch { get; set; } = null!;
    public long BatchId { get; set; }

    public required int BatchPosition { get; set; }
}

public class SelectionEntryPickedEntity : SelectionEntryEntity
{
    public SelectionCandidateEntity Candidate { get; set; } = null!;
    public Guid CandidateId { get; set; }

    public required int Cycle { get; set; }
}

public class SelectionEntryEventEntity : SelectionEntryEntity
{
    public SelectionEventKind Kind { get; set; }
}

internal class SelectionEntryEntityConfiguration :
    IEntityTypeConfiguration<SelectionEntryEntity>,
    IEntityTypeConfiguration<SelectionEntryPickedEntity>,
    IEntityTypeConfiguration<SelectionEntryEventEntity>
{
    public void Configure(EntityTypeBuilder<SelectionEntryEntity> builder)
    {
        builder.UseTphMappingStrategy();

        builder.HasDiscriminator<string>("Type")
            .HasValue<SelectionEntryPickedEntity>("Pick")
            .HasValue<SelectionEntryEventEntity>("Event");

        builder.Property("Type")
            .HasMaxLength(30);
    }
    
    // The entity type configuration of type 'SelectionEntryEntityConfiguration' is invalid. The configuration specified using EntityTypeConfigurationAttribute has to implement 'IEntityTypeConfiguration<SelectionEntryEventEntity>'.

    public void Configure(EntityTypeBuilder<SelectionEntryPickedEntity> builder)
    {
        // Do nothing?
    }

    public void Configure(EntityTypeBuilder<SelectionEntryEventEntity> builder)
    {
        // Do nothing?
    }
}