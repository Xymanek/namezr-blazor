using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Automation;

public interface IFieldAutomationProcessor
{
    FieldAutomationType AutomationType { get; }

    /// <param name="submission"></param>
    /// <param name="fieldConfig">
    /// <see cref="P:Namezr.Features.Questionnaires.Data.QuestionnaireFieldConfigurationEntity.Field"/>
    /// must be loaded
    /// </param>
    /// <param name="fieldValue"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ProcessAsync(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldConfigurationEntity fieldConfig,
        QuestionnaireFieldValueEntity fieldValue,
        CancellationToken cancellationToken = default
    );
}