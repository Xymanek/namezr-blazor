using Namezr.Features.Identity.Data;

namespace Namezr.Features.SelectionSeries.Data;

public class SelectionUserDataEntity
{
    public SelectionSeriesEntity Series { get; set; } = null!;
    public long SeriesId { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Guid UserId { get; set; }

    public required decimal LatestModifier { get; set; }

    public required int SelectedCount { get; set; }
    public required int TotalSelectedCount { get; set; }
}