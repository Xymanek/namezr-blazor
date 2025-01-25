using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Public.Questionnaires;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Pages;
using Namezr.Features.Questionnaires.Services;
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
        IFieldValueSerializer fieldValueSerializer,
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

        Dictionary<Guid, QuestionnaireFieldConfigurationEntity> fieldConfigsById
            = questionnaireVersion.Fields!.ToDictionary(x => x.Field.Id, x => x);

        QuestionnaireSubmissionEntity entity = new()
        {
            VersionId = model.QuestionnaireVersionId,
            SubmittedAt = clock.GetCurrentInstant(),

            FieldValues = model.Values
                .Select(pair => new QuestionnaireFieldValueEntity
                {
                    FieldId = pair.Key,
                    ValueSerialized = fieldValueSerializer.Serialize(fieldConfigsById[pair.Key].Field.Type, pair.Value),
                })
                .ToHashSet(),
        };

        dbContext.QuestionnaireSubmissions.Add(entity);

        await dbContext.SaveChangesAsync(ct);

        return entity.Id;
    }
}