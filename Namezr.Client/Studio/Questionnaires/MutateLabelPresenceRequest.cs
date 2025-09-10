using Namezr.Client.Contracts.Auth;

namespace Namezr.Client.Studio.Questionnaires;

public record MutateLabelPresenceRequest : ISubmissionManagementRequest
{
    public required Guid SubmissionId { get; init; }
    public required Guid LabelId { get; init; }
    
    public required bool NewPresent { get; init; }
}