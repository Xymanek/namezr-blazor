namespace Namezr.Client.Studio.Questionnaires;

public record SetSubmissionAttributeRequest
{
    public required Guid SubmissionId { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; } // Empty value means delete
}