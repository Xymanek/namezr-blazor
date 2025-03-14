using System.Text.Json;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Features.Questionnaires.Services;

public interface IFieldValueSerializer
{
    string Serialize(QuestionnaireFieldType fieldType, SubmissionValueModel value);
    SubmissionValueModel Deserialize(QuestionnaireFieldType fieldType, string valueSerialized);
}

[RegisterSingleton]
internal class FieldValueSerializer : IFieldValueSerializer
{
    // Note that currently only the field type is expected to be immutable.
    // If we need to depend on other options to control serialization, we
    // will need to read QuestionnaireFieldConfigurationEntity

    public string Serialize(QuestionnaireFieldType fieldType, SubmissionValueModel value)
    {
        return fieldType switch
        {
            QuestionnaireFieldType.Text => JsonSerializer.Serialize(value.StringValue),
            QuestionnaireFieldType.Number => JsonSerializer.Serialize(value.NumberValue),
            QuestionnaireFieldType.FileUpload => JsonSerializer.Serialize(value.FileValue ?? []),

            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public SubmissionValueModel Deserialize(QuestionnaireFieldType fieldType, string valueSerialized)
    {
        return fieldType switch
        {
            QuestionnaireFieldType.Text => new SubmissionValueModel
            {
                StringValue = JsonSerializer.Deserialize<string>(valueSerialized) ??
                              throw new Exception("Deserialized string is null"),
            },

            QuestionnaireFieldType.Number => new SubmissionValueModel
            {
                NumberValue = JsonSerializer.Deserialize<decimal>(valueSerialized),
            },

            QuestionnaireFieldType.FileUpload => new SubmissionValueModel
            {
                FileValue = JsonSerializer.Deserialize<List<SubmissionFileData>>(valueSerialized) ??
                            throw new Exception("Deserialized list is null"),
            },

            _ => throw new ArgumentOutOfRangeException()
        };
    }
}