namespace Namezr.Client.Shared;

public record ToggleSubmissionApprovalRequest
{
    public required Guid SubmissionId { get; init; }
    public required bool ShouldApprove { get; init; }
}