namespace Namezr.Client.Studio.Questionnaires.Edit;

// TODO: make these records?

public class QuestionnaireEditModel
{
    public string Title { get; set; }
    public string Description { get; set; }

    public List<QuestionnaireFieldEditModel> Fields { get; } = new();
}

public class QuestionnaireFieldEditModel
{
    public required Guid Id { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }

    public QuestionnaireFieldType Type { get; set; }
}

public enum QuestionnaireFieldType
{
    Text,
    Number,
    FileUpload,
}