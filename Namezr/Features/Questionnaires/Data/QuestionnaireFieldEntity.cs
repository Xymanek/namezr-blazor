using System.ComponentModel.DataAnnotations.Schema;
using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Features.Questionnaires.Data;

public class QuestionnaireFieldEntity
{
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }

    public Guid QuestionnaireId { get; set; }
    public QuestionnaireEntity Questionnaire { get; set; } = null!;

    public required QuestionnaireFieldType Type { get; set; }

    public ICollection<QuestionnaireFieldConfigurationEntity>? Configurations { get; set; }
}
