namespace Namezr.Client.Studio.Questionnaires;

public record MutateLabelPresenceRequest
{
    public required Guid SubmissionId { get; init; }
    public required Guid LabelId { get; init; }
    
    public required bool NewPresent { get; init; }
}