using Namezr.Client.Public.Questionnaires;
using Namezr.Features.Questionnaires.Data;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Questionnaires.Pages;

[Mapper(UseDeepCloning = true, RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class SubmissionMapper
{
    [MapNestedProperties(nameof(QuestionnaireVersionEntity.Questionnaire))]
    public static partial QuestionnaireConfigModel MapToConfigModel(this QuestionnaireVersionEntity source);

    private static List<QuestionnaireConfigFieldModel> MapToConfigModel(
        ICollection<QuestionnaireFieldConfigurationEntity> source
    )
    {
        List<QuestionnaireConfigFieldModel> target = new(source.Count);

        target.AddRange(
            source
                .OrderBy(x => x.Order)
                .Select(MapToConfigModel)
        );

        return target;
    }

    [MapNestedProperties(nameof(QuestionnaireFieldConfigurationEntity.Field))]
    private static partial QuestionnaireConfigFieldModel MapToConfigModel(
        this QuestionnaireFieldConfigurationEntity source
    );
}