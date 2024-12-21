using Namezr.Client.Studio.Questionnaires.Edit;
using Riok.Mapperly.Abstractions;

namespace Namezr.Features.Questionnaires.Data;

[Mapper(UseDeepCloning = true)]
public static partial class QuestionnaireEntityMapper
{
    [MapperIgnoreSource(nameof(QuestionnaireEntity.Id))]
    public static partial QuestionnaireEditModel MapToEditModel(this QuestionnaireEntity source);

    // TODO: does not work for updating (always new instances)
    [MapperIgnoreTarget(nameof(QuestionnaireEntity.Id))]
    public static partial QuestionnaireEntity MapToEntity(this QuestionnaireEditModel source);

    // public static partial void MapToEntity(QuestionnaireFieldEditModel source, QuestionnaireFieldEntity target);
    //
    // [MapperIgnoreTarget(nameof(QuestionnaireEntity.Id))]
    // [MapperIgnoreTarget(nameof(QuestionnaireEntity.Fields))]
    // [MapperIgnoreSource(nameof(QuestionnaireEditModel.Fields))]
    // private static partial void DoMapToEntity(QuestionnaireEditModel source, QuestionnaireEntity target);
    //
    // [UserMapping(Default = true)]
    // public static void MapToEntity(QuestionnaireEditModel source, QuestionnaireEntity target)
    // {
    //     DoMapToEntity(source, target);
    // }
    //
    // private static ICollection<QuestionnaireFieldEntity> s(
    //     List<QuestionnaireFieldEditModel> source,
    //     ICollection<QuestionnaireFieldEntity>? target
    // )
    // {
    //     //
    // }
}