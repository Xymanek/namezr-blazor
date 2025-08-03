using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Automation;

[RegisterScoped]
public partial class FieldAutomationService : IFieldAutomationService
{
    private readonly IReadOnlyDictionary<FieldAutomationType, IFieldAutomationProcessor> _processors;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<FieldAutomationService> _logger;

    public FieldAutomationService(
        IEnumerable<IFieldAutomationProcessor> processors,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ILogger<FieldAutomationService> logger
    )
    {
        _processors = processors.ToDictionary(p => p.AutomationType);
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <param name="submission">
    /// <see cref="P:Namezr.Features.Questionnaires.Data.QuestionnaireSubmissionEntity.FieldValues"/>
    /// must be loaded
    /// </param>
    public void ProcessFieldAutomationInBackground(QuestionnaireSubmissionEntity submission)
    {
        // Process field automation in background
        Task.Run(async () =>
        {
            try
            {
                await ProcessFieldAutomationInternalAsync(submission, CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Log unexpected errors in background processing
                LogUnexpectedErrorInBackgroundProcessing(ex, submission.Id);
            }
        });
    }

    /// <param name="submission">
    /// <see cref="P:Namezr.Features.Questionnaires.Data.QuestionnaireSubmissionEntity.FieldValues"/>
    /// must be loaded
    /// </param>
    /// <param name="cancellationToken"></param>
    private async Task ProcessFieldAutomationInternalAsync(
        QuestionnaireSubmissionEntity submission,
        CancellationToken cancellationToken
    )
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        using Activity? activity = Diagnostics.ActivitySource.StartActivity("ProcessFieldAutomation");
        activity?.SetTag("submission.id", submission.Id.ToString());
        activity?.SetTag("submission.version_id", submission.VersionId.ToString());

        LogStartingFieldAutomationProcessing(submission.Id);

        // Important: do not load submission.FieldValues here as we may have had another update since this was started

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Load field configurations for automation
        Dictionary<Guid, QuestionnaireFieldConfigurationEntity> fieldConfigs = await dbContext.QuestionnaireFieldConfigurations
            .Where(fc => fc.VersionId == submission.VersionId && fc.Automation != null)
            .Include(fc => fc.Field)
            .ToDictionaryAsync(fc => fc.FieldId, cancellationToken);

        activity?.SetTag("field_configs.count", fieldConfigs.Count);
        LogFoundFieldConfigsWithAutomation(fieldConfigs.Count, submission.Id);

        foreach (QuestionnaireFieldValueEntity fieldValue in submission.FieldValues!)
        {
            if (!fieldConfigs.TryGetValue(fieldValue.FieldId, out QuestionnaireFieldConfigurationEntity? fieldConfig))
                continue;

            if (_processors.TryGetValue(fieldConfig.Automation!.Value, out IFieldAutomationProcessor? processor))
            {
                LogProcessingFieldAutomation(fieldConfig.FieldId, fieldConfig.Automation.Value, submission.Id);

                try
                {
                    await processor.ProcessAsync(submission, fieldConfig, fieldValue, cancellationToken);
                    LogSuccessfullyProcessedFieldAutomation(fieldConfig.FieldId, submission.Id);
                }
                catch (Exception ex)
                {
                    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    // Log the error but don't fail the entire submission process
                    LogFieldAutomationFailed(ex, submission.Id, fieldConfig.FieldId, fieldConfig.Automation.Value);
                }
            }
        }

        LogCompletedFieldAutomationProcessing(submission.Id);
    }

    [LoggerMessage(LogLevel.Debug, "Starting field automation processing for submission {SubmissionId}")]
    private partial void LogStartingFieldAutomationProcessing(Guid submissionId);

    [LoggerMessage(LogLevel.Debug, "Found {FieldConfigCount} fields with automation for submission {SubmissionId}")]
    private partial void LogFoundFieldConfigsWithAutomation(int fieldConfigCount, Guid submissionId);

    [LoggerMessage(LogLevel.Debug, "Processing field {FieldId} with automation type {AutomationType} for submission {SubmissionId}")]
    private partial void LogProcessingFieldAutomation(Guid fieldId, FieldAutomationType automationType, Guid submissionId);

    [LoggerMessage(LogLevel.Debug, "Successfully processed field {FieldId} automation for submission {SubmissionId}")]
    private partial void LogSuccessfullyProcessedFieldAutomation(Guid fieldId, Guid submissionId);

    [LoggerMessage(LogLevel.Error, "Field automation failed for submission {SubmissionId}, field {FieldId}, automation type {AutomationType}")]
    private partial void LogFieldAutomationFailed(Exception ex, Guid submissionId, Guid fieldId, FieldAutomationType automationType);

    [LoggerMessage(LogLevel.Error, "Unexpected error in background field automation processing for submission {SubmissionId}")]
    private partial void LogUnexpectedErrorInBackgroundProcessing(Exception ex, Guid submissionId);

    [LoggerMessage(LogLevel.Debug, "Completed field automation processing for submission {SubmissionId}")]
    private partial void LogCompletedFieldAutomationProcessing(Guid submissionId);
}