using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Shared;
using Namezr.Client.Types;

namespace Namezr.Client.Studio.Questionnaires.Submissions;

public class SubmissionTableItemModel
{
    public required Guid Id { get; init; }
    public required int Number { get; init; }

    public required string UserDisplayName { get; init; }
    public required EligibilityResultModel Eligibility { get; init; }

    public required DateTimeOffset InitiallySubmittedAt { get; init; }
    public required DateTimeOffset LastUpdateAt { get; init; }
    
    public required bool IsApproved { get; init; }
    
    public required IReadOnlyList<SubmissionLabelModel> Labels { get; init; }
    public required IReadOnlyDictionary<Guid, SubmissionValueModel> Values { get; set; }
}