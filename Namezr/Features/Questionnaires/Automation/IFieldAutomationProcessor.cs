using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Automation;

public interface IFieldAutomationProcessor
{
    FieldAutomationType AutomationType { get; }
    
    Task ProcessAsync(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldConfigurationEntity fieldConfig,
        QuestionnaireFieldValueEntity fieldValue,
        CancellationToken cancellationToken = default
    );
}