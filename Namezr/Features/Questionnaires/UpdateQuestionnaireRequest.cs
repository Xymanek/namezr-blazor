using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires;

[Handler]
[MapPost(ApiEndpointPaths.QuestionnairesUpdate)]
internal static partial class UpdateQuestionnaireRequest
{
    private static async ValueTask HandleAsync(
        UpdateQuestionnaireCommand request,
        ApplicationDbContext dbContext,
        CancellationToken ct
    )
    {
        QuestionnaireEntity? questionnaireEntity = await dbContext.Questionnaires
            .Include(x => x.Versions)
            .Include(x => x.Fields)
            .AsSplitQuery()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (questionnaireEntity is null)
        {
            // TODO: return 400
            throw new Exception("Questionnaire not found");
        }

        // TODO: broken when load existing -> add new field -> move new to 1st
        new QuestionnaireFormToEntityMapper(questionnaireEntity.Fields!)
            .UpdateEntityWithNewVersion(request.Model, questionnaireEntity);

        await dbContext.SaveChangesAsync(ct);
    }
}