namespace Namezr.Client.Studio.Questionnaires;

public record GetSubmissionAttributeKeysResponse
{
    public required List<string> Keys { get; init; }
}