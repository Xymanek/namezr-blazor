namespace Namezr.Client.Studio.Questionnaires.Selection;

public class ManualAddEntriesResponse
{
    public required int AddedCount { get; init; }
    public required SkippedSubmissionInfo[] SkippedSubmissions { get; init; }
}

public class SkippedSubmissionInfo
{
    public required Guid SubmissionId { get; init; }
    public required int SubmissionNumber { get; init; }
    public required string UserDisplayName { get; init; }
    public required string Reason { get; init; }
}