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
internal partial class X2CharacterPoolProcessor : IFieldAutomationProcessor
{
    private const string AttributeKeyCharacterCount = "x2_character_count";
    private const string AttributeKeyFirstName = "xcom2.character.first_name";
    private const string AttributeKeyLastName = "xcom2.character.last_name";
    private const string AttributeKeyNickname = "xcom2.character.nickname";
    private const string AttributeKeyClass = "xcom2.character.class";
    private const string AttributeKeyCountry = "xcom2.character.country";
    private const string AttributeKeyBackgroundText = "xcom2.character.background_text";
    private const string AttributeKeyAllowedTypeSoldier = "xcom2.character.allowed_type_soldier";
    private const string AttributeKeyAllowedTypeVip = "xcom2.character.allowed_type_vip";
    private const string AttributeKeyAllowedTypeDarkVip = "xcom2.character.allowed_type_dark_vip";
    private const string AttributeKeyAppearanceManagerPrefix = "xcom2.character.am";

    private static readonly IReadOnlySet<string> AttributeKeys = new HashSet<string>
    {
        AttributeKeyCharacterCount,
        AttributeKeyFirstName,
        AttributeKeyLastName,
        AttributeKeyNickname,
        AttributeKeyClass,
        AttributeKeyCountry,
        AttributeKeyBackgroundText,
        AttributeKeyAllowedTypeSoldier,
        AttributeKeyAllowedTypeVip,
        AttributeKeyAllowedTypeDarkVip,
    };

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFieldValueSerializer _fieldValueSerializer;
    private readonly IClock _clock;
    private readonly ILogger<X2CharacterPoolProcessor> _logger;
    private readonly IAttributeUpdaterService _attributeUpdater;

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
        using Activity? activity = Diagnostics.ActivitySource
            .StartActivity("X2CharacterPoolProcessor.ProcessSingleFile");
        activity?.SetTag("file.id", fileData.Id.ToString());
        activity?.SetTag("file.name", fileData.Name);
        activity?.SetTag("file.size_bytes", fileData.SizeBytes);

        LogProcessingFile(fileData.Name, fileData.Id, submission.Id);

        bool clearCharacterAttributes = true;
        bool clearCharCountAttribute = true; // TODO

        try
        {
            await using FileStream fileStream = _fileStorageService.OpenRead(fileData.Id);
            X2BinReader reader = await X2BinFileReadInitializer.InitializeAsync(fileStream);

            CharacterPool characterPool = await CharacterPool.Load(reader);

            int characterCount = characterPool.NativeCharacters.Count;
            activity?.SetTag("character.count", characterCount);

            LogSuccessfullyParsedFile(fileData.Name, characterCount);

            // Add submission attribute with character count
            await _attributeUpdater.StageAttributeUpdateAsync(
                new AttributeUpdateCommand
                {
                    InstigatorIsProgrammatic = true,
                    SubmissionId = submission.Id,
                    Key = AttributeKeyCharacterCount,
                    Value = characterCount.ToString(),
                },
                dbContext,
                cancellationToken
            );
            clearCharCountAttribute = false;

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
                clearCharacterAttributes = false;

                CharacterPoolDataElement character = characterPool.NativeCharacters[0];
                activity?.SetTag("character.first_name", character.FirstName);
                activity?.SetTag("character.last_name", character.LastName);
                activity?.SetTag("character.class", character.SoldierClassTemplateName);
                activity?.SetTag("has_biography", true);

                LogAddingCharacterBiography($"{character.FirstName} {character.LastName}", fileData.Name);
                // TODO: remove all existing AM attributes

                // Add all character properties as submission attributes
                List<AttributeUpdateCommand> attributes =
                [
                    new()
                    {
                        InstigatorIsProgrammatic = true,
                        SubmissionId = submission.Id,
                        Key = AttributeKeyFirstName,
                        Value = character.FirstName ?? ""
                    },
                    new()
                    {
                        InstigatorIsProgrammatic = true,
                        SubmissionId = submission.Id,
                        Key = AttributeKeyLastName,
                        Value = character.LastName ?? ""
                    },
                    new()
                    {
                        InstigatorIsProgrammatic = true,
                        SubmissionId = submission.Id,
                        Key = AttributeKeyNickname,
                        Value = character.NickName ?? ""
                    },
                    new()
                    {
                        InstigatorIsProgrammatic = true,
                        SubmissionId = submission.Id,
                        Key = AttributeKeyClass,
                        Value = character.SoldierClassTemplateName ?? ""
                    },
                    new()
                    {
                        InstigatorIsProgrammatic = true,
                        SubmissionId = submission.Id,
                        Key = AttributeKeyCountry,
                        Value = character.Country ?? ""
                    },
                    new()
                    {
                        InstigatorIsProgrammatic = true,
                        SubmissionId = submission.Id,
                        Key = AttributeKeyBackgroundText,
                        Value = character.BackgroundText ?? ""
                    },
                    new()
                    {
                        InstigatorIsProgrammatic = true,
                        SubmissionId = submission.Id,
                        Key = AttributeKeyAllowedTypeSoldier,
                        Value = character.AllowedTypeSoldier.ToString()
                    },
                    new()
                    {
                        InstigatorIsProgrammatic = true,
                        SubmissionId = submission.Id,
                        Key = AttributeKeyAllowedTypeVip,
                        Value = character.AllowedTypeVIP.ToString()
                    },
                    new()
                    {
                        InstigatorIsProgrammatic = true,
                        SubmissionId = submission.Id,
                        Key = AttributeKeyAllowedTypeDarkVip,
                        Value = character.AllowedTypeDarkVIP.ToString()
                    }
                ];

                // Add appearance data from ExtraDatas (single character pool has only one entry)
                // Could be 0 if non-AM character pool
                if (characterPool.ExtraDatas.Count > 0)
                {
                    ExtraDataEntry extraData = characterPool.ExtraDatas[0];

                    await RemoveAbsentArmorAttributes(
                        dbContext,
                        extraData.AppearanceStore,
                        submission,
                        cancellationToken
                    );

                    foreach (AppearanceInfoStruct appearanceInfo in extraData.AppearanceStore)
                    {
                        foreach ((string key, string value) in appearanceInfo.Appearance.Values)
                        {
                            attributes.Add(new AttributeUpdateCommand
                            {
                                InstigatorIsProgrammatic = true,
                                SubmissionId = submission.Id,
                                Key = $"{GetAppearanceInfoPrefix(appearanceInfo)}.{key}",
                                Value = value
                            });
                        }
                    }
                }

                foreach (AttributeUpdateCommand update in attributes)
                {
                    await _attributeUpdater.StageAttributeUpdateAsync(update, dbContext, CancellationToken.None);
                }

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
            string errorMessage = $"❌ XCOM2 Character Pool validation failed for file '{fileData.Name}'";

            dbContext.SubmissionHistoryEntries.Add(new SubmissionHistoryInternalNoteEntity
            {
                SubmissionId = submission.Id,
                Content = errorMessage,
                OccuredAt = _clock.GetCurrentInstant(),
                InstigatorIsStaff = false,
                InstigatorIsProgrammatic = true,
            });
        }

        // This handles cases like valid submission (with existing attributes) updated with an invalid file

        if (clearCharCountAttribute)
        {
            await _attributeUpdater.StageAttributeUpdateAsync(
                new AttributeUpdateCommand
                {
                    InstigatorIsProgrammatic = true,
                    SubmissionId = submission.Id,
                    Key = AttributeKeyCharacterCount,
                    Value = string.Empty,
                },
                dbContext,
                cancellationToken
            );
        }

        if (clearCharacterAttributes)
        {
            await RemoveAllCharacterAttributes(dbContext, submission, cancellationToken);
        }
    }

    [LoggerMessage(
        LogLevel.Debug,
        "Starting X2 Character Pool processing for submission {SubmissionId}, field {FieldId}"
    )]
    private partial void LogStartingX2Processing(Guid submissionId, Guid fieldId);

    [LoggerMessage(LogLevel.Debug, "Skipping field {FieldId} - not a file upload field")]
    private partial void LogSkippingNotFileUpload(Guid fieldId);

    [LoggerMessage(LogLevel.Debug, "Skipping field {FieldId} - no files to process")]
    private partial void LogSkippingNoFiles(Guid fieldId);

    [LoggerMessage(LogLevel.Debug, "Processing {FileCount} files for submission {SubmissionId}, field {FieldId}")]
    private partial void LogProcessingFiles(int fileCount, Guid submissionId, Guid fieldId);

    [LoggerMessage(
        LogLevel.Debug,
        "Completed X2 Character Pool processing for submission {SubmissionId}, field {FieldId}"
    )]
    private partial void LogCompletedX2Processing(Guid submissionId, Guid fieldId);

    [LoggerMessage(
        LogLevel.Debug,
        "Processing X2 character pool file {FileName} ({FileId}) for submission {SubmissionId}"
    )]
    private partial void LogProcessingFile(string fileName, Guid fileId, Guid submissionId);

    [LoggerMessage(
        LogLevel.Information,
        "Successfully parsed X2 character pool file {FileName} - found {CharacterCount} characters"
    )]
    private partial void LogSuccessfullyParsedFile(string fileName, int characterCount);

    [LoggerMessage(LogLevel.Debug, "Adding character biography for {CharacterName} ({FileName})")]
    private partial void LogAddingCharacterBiography(string characterName, string fileName);

    [LoggerMessage(LogLevel.Debug, "Skipping character biography - {CharacterCount} characters found in {FileName}")]
    private partial void LogSkippingCharacterBiography(int characterCount, string fileName);

    [LoggerMessage(
        LogLevel.Warning,
        "X2 character pool validation failed for file {FileName} ({FileId}) in submission {SubmissionId}"
    )]
    private partial void LogValidationFailed(Exception ex, string fileName, Guid fileId, Guid submissionId);

    private async Task RemoveAllCharacterAttributes(
        ApplicationDbContext dbContext,
        QuestionnaireSubmissionEntity submission,
        CancellationToken cancellationToken
    )
    {
        LogRemovingCharacterAttributes(submission.Id);

        foreach (string key in AttributeKeys)
        {
            await _attributeUpdater.StageAttributeUpdateAsync(
                new AttributeUpdateCommand
                {
                    InstigatorIsProgrammatic = true,
                    SubmissionId = submission.Id,
                    Key = key,
                    Value = string.Empty, // Empty string is semantically same as deleted
                },
                dbContext,
                cancellationToken
            );
        }

        SubmissionAttributeEntity[] appearanceManagerAttributes = await dbContext.SubmissionAttributes
            .AsTracking()
            .Where(a => a.SubmissionId == submission.Id)
            .Where(a => a.Key.ToLower().StartsWith(AttributeKeyAppearanceManagerPrefix.ToLower()))
            .ToArrayAsync(cancellationToken);

        foreach (SubmissionAttributeEntity attribute in appearanceManagerAttributes)
        {
            await _attributeUpdater.StageAttributeUpdateAsync(
                new AttributeUpdateCommand
                {
                    InstigatorIsProgrammatic = true,
                    SubmissionId = submission.Id,
                    Key = attribute.Key,
                    Value = string.Empty, // Empty string is semantically same as deleted
                },
                dbContext,
                cancellationToken
            );
        }
    }

    [LoggerMessage(LogLevel.Debug, "Removing all character attributes for submission {SubmissionId}")]
    private partial void LogRemovingCharacterAttributes(Guid submissionId);

    private async Task RemoveAbsentArmorAttributes(
        ApplicationDbContext dbContext,
        IEnumerable<AppearanceInfoStruct> presentAppearances,
        QuestionnaireSubmissionEntity submission,
        CancellationToken cancellationToken
    )
    {
        IQueryable<SubmissionAttributeEntity> query = dbContext.SubmissionAttributes
                .AsTracking()
                .Where(a => a.SubmissionId == submission.Id)

                // Only consider Appearance Manager attributes
                .Where(a => a.Key.ToLower().StartsWith(AttributeKeyAppearanceManagerPrefix.ToLower()))
            ;

        // Exclude all attributes that start with the present appearance info prefixes
        // ReSharper disable once LoopCanBeConvertedToQuery - much harder to read
        foreach (AppearanceInfoStruct presentAppearance in presentAppearances)
        {
            string appearancePrefix = GetAppearanceInfoPrefix(presentAppearance);
            query = query.Where(a => !a.Key.ToLower().StartsWith(appearancePrefix.ToLower()));
        }

        SubmissionAttributeEntity[] appearanceManagerAttributes = await query
            .ToArrayAsync(cancellationToken);

        foreach (SubmissionAttributeEntity attribute in appearanceManagerAttributes)
        {
            await _attributeUpdater.StageAttributeUpdateAsync(
                new AttributeUpdateCommand
                {
                    InstigatorIsProgrammatic = true,
                    SubmissionId = submission.Id,
                    Key = attribute.Key,
                    Value = string.Empty, // Empty string is semantically same as deleted
                },
                dbContext,
                cancellationToken
            );
        }
    }

    private static string GetAppearanceInfoPrefix(AppearanceInfoStruct appearanceInfo)
    {
        return AttributeKeyAppearanceManagerPrefix + "." + appearanceInfo.GenderArmorTemplate;
    }
}