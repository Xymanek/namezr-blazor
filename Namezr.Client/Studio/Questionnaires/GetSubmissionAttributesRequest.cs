using Namezr.Client.Shared;

namespace Namezr.Client.Studio.Questionnaires;

public record GetSubmissionAttributesRequest
{
    public required Guid SubmissionId { get; init; }
}

public record GetSubmissionAttributesResponse
{
    public required List<SubmissionAttributeModel> Attributes { get; init; }
}