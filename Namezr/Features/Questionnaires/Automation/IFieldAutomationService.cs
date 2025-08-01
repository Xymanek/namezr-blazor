using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Automation;

public interface IFieldAutomationService
{
    void ProcessFieldAutomationInBackground(QuestionnaireSubmissionEntity submission);
}