using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Automation;

public interface IFieldAutomationService
{
    void ProcessFieldAutomationAsync(QuestionnaireSubmissionEntity submission);
}