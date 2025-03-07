using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Namezr.Features.Identity.Data;

namespace Namezr.Features.SelectionSeries.Data;

[EntityTypeConfiguration(typeof(SelectionUserDataEntityConfiguration))]
public class SelectionUserDataEntity
{
    public SelectionSeriesEntity Series { get; set; } = null!;
    public Guid SeriesId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Guid UserId { get; set; }

    public required decimal LatestModifier { get; set; }

    public required int SelectedCount { get; set; }
    public required int TotalSelectedCount { get; set; }
}

internal class SelectionUserDataEntityConfiguration : IEntityTypeConfiguration<SelectionUserDataEntity>
{
    public void Configure(EntityTypeBuilder<SelectionUserDataEntity> builder)
    {
        builder.HasKey(e => new { e.SeriesId, e.UserId });
    }
}