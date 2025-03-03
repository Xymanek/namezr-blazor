namespace Namezr.Features.SelectionSeries.Data;

public class SelectionEntryEntity
{
    public long Id { get; set; }

    public SelectionBatchEntity Batch { get; set; } = null!;
    public long BatchId { get; set; }

    public required int Cycle { get; set; }
    public required int BatchPosition { get; set; }
    
    public SelectionCandidateEntity Candidate { get; set; } = null!;
    public Guid CandidateId { get; set; }
}