using Namezr.Client.Studio.Questionnaires.Edit;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Questionnaires.Data;

public class QuestionnaireFieldEntity
{
    public QuestionnaireFieldId Id { get; set; }

    [MapperIgnore] public QuestionnaireId QuestionnaireId { get; set; }
    [MapperIgnore] public QuestionnaireEntity Questionnaire { get; set; } = null!;

    public required QuestionnaireFieldType Type { get; set; }

    public ICollection<QuestionnaireFieldConfigurationEntity>? Configurations { get; set; }
}
