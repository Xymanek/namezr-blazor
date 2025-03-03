namespace Namezr.Features.SelectionSeries.Data;

public class SelectionEventEntity
{
    public long Id { get; set; }

    public SelectionBatchEntity Batch { get; set; } = null!;
    public long BatchId { get; set; }

    public required int BatchPosition { get; set; }
    
    public SelectionEventKind Kind { get; set; }
}