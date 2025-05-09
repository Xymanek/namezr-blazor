namespace Namezr.Features.Questionnaires.Data;

public class SubmissionLabelLinkEntity
{
    public QuestionnaireSubmissionEntity Submission { get; set; } = null!;
    public Guid SubmissionId { get; set; }
    
    public SubmissionLabelEntity Label { get; set; } = null!;
    public Guid LabelId { get; set; }
}