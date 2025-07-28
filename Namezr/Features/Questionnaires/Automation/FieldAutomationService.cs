using AutoRegisterInject;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Automation;

[RegisterScoped]
public class FieldAutomationService : IFieldAutomationService
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

    public void ProcessFieldAutomationAsync(QuestionnaireSubmissionEntity submission)
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
                _logger.LogError(ex, 
                    "Unexpected error in background field automation processing for submission {SubmissionId}",
                    submission.Id);
            }
        });
    }

    private async Task ProcessFieldAutomationInternalAsync(
        QuestionnaireSubmissionEntity submission,
        CancellationToken cancellationToken
    )
    {
        if (submission.FieldValues == null)
            return;

        using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Load field configurations for automation
        Dictionary<Guid, QuestionnaireFieldConfigurationEntity> fieldConfigs = await dbContext.QuestionnaireFieldConfigurations
            .Where(fc => fc.VersionId == submission.VersionId && fc.Automation != null)
            .ToDictionaryAsync(fc => fc.FieldId, cancellationToken);

        foreach (QuestionnaireFieldValueEntity fieldValue in submission.FieldValues)
        {
            if (!fieldConfigs.TryGetValue(fieldValue.FieldId, out QuestionnaireFieldConfigurationEntity? fieldConfig))
                continue;

            if (_processors.TryGetValue(fieldConfig.Automation!.Value, out IFieldAutomationProcessor? processor))
            {
                try
                {
                    await processor.ProcessAsync(submission, fieldConfig, fieldValue, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the entire submission process
                    _logger.LogError(ex, 
                        "Field automation failed for submission {SubmissionId}, field {FieldId}, automation type {AutomationType}",
                        submission.Id, fieldConfig.FieldId, fieldConfig.Automation);
                }
            }
        }
    }
}