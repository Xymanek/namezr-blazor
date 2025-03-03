using Riok.Mapperly.Abstractions;

namespace Namezr.Client.Public.Questionnaires;

[Mapper(UseDeepCloning = true)]
public static partial class FieldValuesCloner
{
    public static partial void DeepCloneInto(
        this Dictionary<string, SubmissionValueModel> source,
        Dictionary<string, SubmissionValueModel> target
    );
}