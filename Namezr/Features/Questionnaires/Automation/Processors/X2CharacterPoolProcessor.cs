using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Files.Services;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;
using NodaTime;
using X2CharacterPool.Domain;
using X2CharacterPool.Serialization;

namespace Namezr.Features.Questionnaires.Automation.Processors;

[RegisterScoped]
[AutoConstructor]
public partial class X2CharacterPoolProcessor : IFieldAutomationProcessor
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFieldValueSerializer _fieldValueSerializer;
    private readonly IClock _clock;
    private readonly ILogger<X2CharacterPoolProcessor> _logger;

    public FieldAutomationType AutomationType => FieldAutomationType.X2CharacterBin;

    public async Task ProcessAsync(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldConfigurationEntity fieldConfig,
        QuestionnaireFieldValueEntity fieldValue,
        CancellationToken cancellationToken = default
    )
    {
        // ReSharper disable once ExplicitCallerInfoArgument
        using Activity? activity = Diagnostics.ActivitySource.StartActivity("X2CharacterPoolProcessor.ProcessAsync");
        activity?.SetTag("submission.id", submission.Id.ToString());
        activity?.SetTag("field.id", fieldConfig.FieldId.ToString());

        LogStartingX2Processing(submission.Id, fieldConfig.FieldId);

        // Only process file upload fields
        if (fieldConfig.Field.Type != QuestionnaireFieldType.FileUpload)
        {
            activity?.SetTag("skipped.reason", "not_file_upload");
            LogSkippingNotFileUpload(fieldConfig.FieldId);
            return;
        }

        // Parse the field value to get uploaded files
        SubmissionValueModel submissionValue = _fieldValueSerializer.Deserialize(
            fieldConfig.Field.Type,
            fieldValue.ValueSerialized
        );

        if (submissionValue.FileValue == null || submissionValue.FileValue.Count == 0)
        {
            activity?.SetTag("skipped.reason", "no_files");
            LogSkippingNoFiles(fieldConfig.FieldId);
            return;
        }

        activity?.SetTag("files.count", submissionValue.FileValue.Count);
        LogProcessingFiles(submissionValue.FileValue.Count, submission.Id, fieldConfig.FieldId);

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        foreach (SubmissionFileData fileData in submissionValue.FileValue)
        {
            await ProcessSingleFileAsync(submission, fileData, dbContext, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        LogCompletedX2Processing(submission.Id, fieldConfig.FieldId);
    }

    private async Task ProcessSingleFileAsync(
        QuestionnaireSubmissionEntity submission,
        SubmissionFileData fileData,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        using Activity? activity = Diagnostics.ActivitySource.StartActivity("X2CharacterPoolProcessor.ProcessSingleFile");
        activity?.SetTag("file.id", fileData.Id.ToString());
        activity?.SetTag("file.name", fileData.Name);
        activity?.SetTag("file.size_bytes", fileData.SizeBytes);

        LogProcessingFile(fileData.Name, fileData.Id, submission.Id);

        try
        {
            using FileStream fileStream = _fileStorageService.OpenRead(fileData.Id);
            X2BinReader reader = await X2BinFileReadInitializer.InitializeAsync(fileStream);

            CharacterPool characterPool = await CharacterPool.Load(reader);

            int characterCount = characterPool.NativeCharacters.Count;
            activity?.SetTag("character.count", characterCount);

            LogSuccessfullyParsedFile(fileData.Name, characterCount);

            // Add submission attribute with character count
            SubmissionAttributeEntity? existingAttribute = await dbContext.SubmissionAttributes
                .FirstOrDefaultAsync(
                    a => a.SubmissionId == submission.Id && a.Key == "x2_character_count",
                    cancellationToken
                );

            if (existingAttribute != null)
            {
                existingAttribute.Value = characterCount.ToString();
            }
            else
            {
                dbContext.SubmissionAttributes.Add(new SubmissionAttributeEntity
                {
                    SubmissionId = submission.Id,
                    Key = "x2_character_count",
                    Value = characterCount.ToString(),
                });
            }

            activity?.SetTag("validation.success", true);

            // Add validation success comment
            string validationMessage = $"✅ XCOM2 Character Pool validation successful for file '{fileData.Name}'\n" +
                                       $"Characters found: {characterCount}";

            dbContext.SubmissionHistoryEntries.Add(new SubmissionHistoryInternalNoteEntity
            {
                SubmissionId = submission.Id,
                Content = validationMessage,
                OccuredAt = _clock.GetCurrentInstant(),
                InstigatorIsStaff = false,
                InstigatorIsProgrammatic = true,
            });

            // If single character, add all character properties as submission attributes
            if (characterCount == 1)
            {
                CharacterPoolDataElement character = characterPool.NativeCharacters[0];
                activity?.SetTag("character.first_name", character.FirstName);
                activity?.SetTag("character.last_name", character.LastName);
                activity?.SetTag("character.class", character.SoldierClassTemplateName);
                activity?.SetTag("has_biography", true);

                LogAddingCharacterBiography($"{character.FirstName} {character.LastName}", fileData.Name);

                // Add all character properties as submission attributes
                List<SubmissionAttributeEntity> attributes =
                [
                    new()
                    {
                        SubmissionId = submission.Id, Key = "xcom2.character.first_name",
                        Value = character.FirstName ?? ""
                    },
                    new()
                    {
                        SubmissionId = submission.Id, Key = "xcom2.character.last_name",
                        Value = character.LastName ?? ""
                    },
                    new()
                    {
                        SubmissionId = submission.Id, Key = "xcom2.character.nickname", Value = character.NickName ?? ""
                    },
                    new()
                    {
                        SubmissionId = submission.Id, Key = "xcom2.character.class",
                        Value = character.SoldierClassTemplateName ?? ""
                    },
                    new()
                    {
                        SubmissionId = submission.Id, Key = "xcom2.character.country", Value = character.Country ?? ""
                    },
                    new()
                    {
                        SubmissionId = submission.Id, Key = "xcom2.character.background_text",
                        Value = character.BackgroundText ?? ""
                    },
                    new()
                    {
                        SubmissionId = submission.Id, Key = "xcom2.character.allowed_type_soldier",
                        Value = character.AllowedTypeSoldier.ToString()
                    },
                    new()
                    {
                        SubmissionId = submission.Id, Key = "xcom2.character.allowed_type_vip",
                        Value = character.AllowedTypeVIP.ToString()
                    },
                    new()
                    {
                        SubmissionId = submission.Id, Key = "xcom2.character.allowed_type_dark_vip",
                        Value = character.AllowedTypeDarkVIP.ToString()
                    }
                ];

                // Add appearance data from ExtraDatas (single character pool has only one entry)
                // Could be 0 if non-AM character pool
                if (characterPool.ExtraDatas.Count > 0)
                {
                    ExtraDataEntry extraData = characterPool.ExtraDatas[0];
                    
                    foreach (AppearanceInfoStruct appearanceInfo in extraData.AppearanceStore)
                    {
                        foreach ((string key, string value) in appearanceInfo.Appearance.Values)
                        {
                            attributes.Add(new SubmissionAttributeEntity
                            {
                                SubmissionId = submission.Id,
                                Key = $"xcom2.character.am.{appearanceInfo.GenderArmorTemplate}.{key}",
                                Value = value
                            });
                        }
                    }
                }

                dbContext.SubmissionAttributes.AddRange(attributes);

                StringBuilder biographyBuilder = new();

                biographyBuilder.AppendLine($"📋 Character Biography for '{fileData.Name}':");
                biographyBuilder.AppendLine($"First Name: {character.FirstName}");
                biographyBuilder.AppendLine($"Last Name: {character.LastName}");
                
                if (!string.IsNullOrWhiteSpace(character.NickName))
                    biographyBuilder.AppendLine($"Nickname: {character.NickName}");

                biographyBuilder.AppendLine($"Class: {character.SoldierClassTemplateName}");
                biographyBuilder.AppendLine($"Country: {character.Country}");
                
                if (!string.IsNullOrWhiteSpace(character.BackgroundText))
                    biographyBuilder.AppendLine($"Background: {character.BackgroundText}");

                biographyBuilder.AppendLine($"Allowed Types:");
                biographyBuilder.AppendLine($"- Soldier: {(character.AllowedTypeSoldier ? "Yes" : "No")}");
                biographyBuilder.AppendLine($"- VIP: {(character.AllowedTypeVIP ? "Yes" : "No")}");
                biographyBuilder.AppendLine($"- Dark VIP: {(character.AllowedTypeDarkVIP ? "Yes" : "No")}");

                dbContext.SubmissionHistoryEntries.Add(new SubmissionHistoryInternalNoteEntity
                {
                    SubmissionId = submission.Id,
                    Content = biographyBuilder.ToString(),
                    OccuredAt = _clock.GetCurrentInstant(),
                    InstigatorIsStaff = false,
                    InstigatorIsProgrammatic = true,
                });
            }
            else
            {
                activity?.SetTag("has_biography", false);
                LogSkippingCharacterBiography(characterCount, fileData.Name);
            }
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("validation.success", false);
            activity?.SetTag("error.message", ex.Message);

            LogValidationFailed(ex, fileData.Name, fileData.Id, submission.Id);

            // Add validation failure comment
            string errorMessage = $"❌ XCOM2 Character Pool validation failed for file '{fileData.Name}'\n" +
                                  $"Error: {ex.Message}";

            dbContext.SubmissionHistoryEntries.Add(new SubmissionHistoryInternalNoteEntity
            {
                SubmissionId = submission.Id,
                Content = errorMessage,
                OccuredAt = _clock.GetCurrentInstant(),
                InstigatorIsStaff = false,
                InstigatorIsProgrammatic = true,
            });
        }
    }

    [LoggerMessage(LogLevel.Debug, "Starting X2 Character Pool processing for submission {SubmissionId}, field {FieldId}")]
    private partial void LogStartingX2Processing(Guid submissionId, Guid fieldId);

    [LoggerMessage(LogLevel.Debug, "Skipping field {FieldId} - not a file upload field")]
    private partial void LogSkippingNotFileUpload(Guid fieldId);

    [LoggerMessage(LogLevel.Debug, "Skipping field {FieldId} - no files to process")]
    private partial void LogSkippingNoFiles(Guid fieldId);

    [LoggerMessage(LogLevel.Debug, "Processing {FileCount} files for submission {SubmissionId}, field {FieldId}")]
    private partial void LogProcessingFiles(int fileCount, Guid submissionId, Guid fieldId);

    [LoggerMessage(LogLevel.Debug, "Completed X2 Character Pool processing for submission {SubmissionId}, field {FieldId}")]
    private partial void LogCompletedX2Processing(Guid submissionId, Guid fieldId);

    [LoggerMessage(LogLevel.Debug, "Processing X2 character pool file {FileName} ({FileId}) for submission {SubmissionId}")]
    private partial void LogProcessingFile(string fileName, Guid fileId, Guid submissionId);

    [LoggerMessage(LogLevel.Information, "Successfully parsed X2 character pool file {FileName} - found {CharacterCount} characters")]
    private partial void LogSuccessfullyParsedFile(string fileName, int characterCount);

    [LoggerMessage(LogLevel.Debug, "Adding character biography for {CharacterName} ({FileName})")]
    private partial void LogAddingCharacterBiography(string characterName, string fileName);

    [LoggerMessage(LogLevel.Debug, "Skipping character biography - {CharacterCount} characters found in {FileName}")]
    private partial void LogSkippingCharacterBiography(int characterCount, string fileName);

    [LoggerMessage(LogLevel.Warning, "X2 character pool validation failed for file {FileName} ({FileId}) in submission {SubmissionId}")]
    private partial void LogValidationFailed(Exception ex, string fileName, Guid fileId, Guid submissionId);
}