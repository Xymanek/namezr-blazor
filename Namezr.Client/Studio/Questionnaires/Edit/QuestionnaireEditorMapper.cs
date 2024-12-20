using Riok.Mapperly.Abstractions;

namespace Namezr.Client.Studio.Questionnaires.Edit;

[Mapper(UseDeepCloning = true)]
public static partial class QuestionnaireEditorMapper
{
    public static partial QuestionnaireEditModel Clone(this QuestionnaireEditModel source);
}