﻿using NodaTime;

namespace Namezr.Features.SelectionSeries.Data;

public class SelectionBatchEntity
{
    public long Id { get; set; }

    public SelectionSeriesEntity Series { get; set; } = null!;
    public long SeriesId { get; set; }

    public required Instant RollStartedAt { get; set; }
    public required Instant RollCompletedAt { get; set; }

    public ICollection<SelectionEntryEntity>? Entries { get; set; }
}