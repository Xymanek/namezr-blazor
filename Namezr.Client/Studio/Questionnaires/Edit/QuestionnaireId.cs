using Vogen;

namespace Namezr.Client.Studio.Questionnaires.Edit;

[ValueObject<Guid>]
public readonly partial struct QuestionnaireId;

[ValueObject<Guid>]
public readonly partial struct QuestionnaireFieldId
{
    public static QuestionnaireFieldId New() => From(Guid.CreateVersion7());
}
