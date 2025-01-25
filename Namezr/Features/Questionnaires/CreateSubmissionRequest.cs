using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Pages;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires;

[Handler]
[MapPost(ApiEndpointPaths.QuestionnaireSubmissionCreate)]
public partial class SubmissionCreateRequest
{
    private static async ValueTask<Guid> HandleAsync(
        SubmissionCreateModel model,
        IClock clock,
        ApplicationDbContext dbContext,
        CancellationToken ct
    )
    {
        QuestionnaireVersionEntity? questionnaireVersion = await dbContext.QuestionnaireVersions
            .AsNoTracking()
            .Include(x => x.Questionnaire)
            .Include(x => x.Fields!).ThenInclude(x => x.Field)
            .SingleOrDefaultAsync(x => x.Id == model.QuestionnaireVersionId, ct);

        if (questionnaireVersion is null)
        {
            // TODO: return 400
            throw new Exception("Questionnaire version not found");
        }

        // TODO: map only the field configs
        QuestionnaireConfigModel configModel = questionnaireVersion.MapToConfigModel();

        // TODO: keys - GUID or string?
        // new SubmissionModelValidator(SubmissionModelValidator.CreateRuleMap(configModel)).ValidateAsync(model);

        Dictionary<Guid,QuestionnaireFieldConfigurationEntity> fieldConfigsById 
            = questionnaireVersion.Fields! .ToDictionary(x => x.Field.Id, x => x);

        QuestionnaireSubmissionEntity entity = new()
        {
            VersionId = model.QuestionnaireVersionId,
            SubmittedAt = clock.GetCurrentInstant(),

            FieldValues = model.Values
                .Select(pair => new QuestionnaireFieldValueEntity
                {
                    FieldId = pair.Key,
                    ValueSerialized = SerializeValue(pair.Value, fieldConfigsById[pair.Key].Field),
                })
                .ToHashSet(),
        };

        dbContext.QuestionnaireSubmissions.Add(entity);

        await dbContext.SaveChangesAsync(ct);

        return entity.Id;
    }

    private static string SerializeValue(SubmissionValueModel value, QuestionnaireFieldEntity field)
    {
        switch (field.Type)
        {
            case QuestionnaireFieldType.Text:
                return JsonSerializer.Serialize(value.StringValue);
            
            case QuestionnaireFieldType.Number:
                return JsonSerializer.Serialize(value.NumberValue);
            
            case QuestionnaireFieldType.FileUpload:
                throw new NotImplementedException();
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}