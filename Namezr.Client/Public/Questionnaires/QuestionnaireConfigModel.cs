using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Client.Public.Questionnaires;

public class QuestionnaireConfigModel
{
    public required string Title { get; init; }
    public required string? Description { get; init; }

    public List<QuestionnaireConfigFieldModel> Fields { get; init; } = new();
}

public class QuestionnaireConfigFieldModel
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }
    public required string? Description { get; init; }
    
    public required QuestionnaireFieldType Type { get; init; }
    
    public required QuestionnaireTextFieldOptionsModel? TextOptions { get; init; }
    public required QuestionnaireNumberFieldOptionsModel? NumberOptions { get; init; }
    public required QuestionnaireFileUploadFieldOptionsModel? FileUploadOptions { get; init; }
}