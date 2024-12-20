namespace Namezr.Client.Studio.Questionnaires.Edit;

public class QuestionnaireEditModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<QuestionnaireFieldEditModel> Fields { get; set; } = new();

    public void AddBlankField()
    {
        Fields.Add(new QuestionnaireFieldEditModel
        {
            Id = Guid.NewGuid(),
        });
    }
}

public class QuestionnaireFieldEditModel
{
    public required Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public QuestionnaireFieldType? Type { get; set; }

    public QuestionnaireTextFieldOptionsModel? TextOptions { get; set; }
    public QuestionnaireNumberFieldOptionsModel? NumberOptions { get; set; }
    public QuestionnaireFileUploadFieldOptionsModel? FileUploadOptions { get; set; }
}

public class QuestionnaireTextFieldOptionsModel
{
    public bool IsMultiline { get; set; }

    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
}

public class QuestionnaireNumberFieldOptionsModel
{
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
}

public class QuestionnaireFileUploadFieldOptionsModel
{
    /// <summary>
    /// If empty - any extension
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new();

    public bool IsMultiple { get; set; }
    
    public decimal? MaxItemSize { get; set; }
    public int? MaxItemCount { get; set; }
}

public enum QuestionnaireFieldType
{
    Text,
    Number,
    FileUpload,
}