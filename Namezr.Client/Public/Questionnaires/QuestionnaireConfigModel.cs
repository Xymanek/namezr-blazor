using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Client.Public.Questionnaires;

public class QuestionnaireConfigModel
{
    public List<QuestionnaireConfigFieldModel> Fields { get; init; } = new();
}

public class QuestionnaireConfigFieldModel
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }
    public required string? Description { get; init; }

    public required QuestionnaireFieldType Type { get; init; }

    public QuestionnaireTextFieldOptionsModel? TextOptions { get; init; }
    public QuestionnaireNumberFieldOptionsModel? NumberOptions { get; init; }
    public QuestionnaireFileUploadFieldOptionsModel? FileUploadOptions { get; init; }
}