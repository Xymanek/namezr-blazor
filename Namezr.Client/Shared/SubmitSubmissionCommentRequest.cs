namespace Namezr.Client.Shared;

public record SubmitSubmissionCommentRequest
{
    public required Guid SubmissionId { get; init; }
    public required string Content { get; init; }
    public required StudioSubmissionCommentType Type { get; init; }
}