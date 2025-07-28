using System.Text;
using AutoRegisterInject;
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
public class X2CharacterPoolProcessor : IFieldAutomationProcessor
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFieldValueSerializer _fieldValueSerializer;
    private readonly IClock _clock;

    public X2CharacterPoolProcessor(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IFileStorageService fileStorageService,
        IFieldValueSerializer fieldValueSerializer,
        IClock clock
    )
    {
        _dbContextFactory = dbContextFactory;
        _fileStorageService = fileStorageService;
        _fieldValueSerializer = fieldValueSerializer;
        _clock = clock;
    }

    public FieldAutomationType AutomationType => FieldAutomationType.X2CharacterBin;

    public async Task ProcessAsync(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldConfigurationEntity fieldConfig,
        QuestionnaireFieldValueEntity fieldValue,
        CancellationToken cancellationToken = default
    )
    {
        // Only process file upload fields
        if (fieldConfig.Field.Type != QuestionnaireFieldType.FileUpload)
            return;

        // Parse the field value to get uploaded files
        SubmissionValueModel submissionValue = _fieldValueSerializer.Deserialize(
            fieldConfig.Field.Type,
            fieldValue.ValueSerialized
        );

        if (submissionValue.FileValue == null || submissionValue.FileValue.Count == 0)
            return;

        using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        foreach (SubmissionFileData fileData in submissionValue.FileValue)
        {
            await ProcessSingleFileAsync(submission, fileData, dbContext, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessSingleFileAsync(
        QuestionnaireSubmissionEntity submission,
        SubmissionFileData fileData,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using FileStream fileStream = _fileStorageService.OpenRead(fileData.Id);
            X2BinReader reader = await X2BinFileReadInitializer.InitializeAsync(fileStream);

            CharacterPool characterPool = await CharacterPool.Load(reader);

            int characterCount = characterPool.NativeCharacters.Count;

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

            // If single character, add biography comment
            if (characterCount == 1)
            {
                CharacterPoolDataElement character = characterPool.NativeCharacters[0];
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
        }
        catch (Exception ex)
        {
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
}