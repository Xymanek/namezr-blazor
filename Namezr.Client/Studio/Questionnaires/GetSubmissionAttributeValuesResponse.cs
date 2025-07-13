namespace Namezr.Client.Studio.Questionnaires;

public record GetSubmissionAttributeValuesResponse
{
    public required List<string> Values { get; init; }
}