using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Client.Studio.Questionnaires;

public class VersionOverviewModel
{
    public required Guid Id { get; init; }
    public required int Number { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public required QuestionnaireFieldEditModel[] Fields { get; init; }
}