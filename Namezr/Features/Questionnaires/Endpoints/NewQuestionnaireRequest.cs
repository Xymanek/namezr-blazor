using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.QuestionnairesNew)]
internal static partial class NewQuestionnaireRequest
{
    private static async ValueTask<Guid> HandleAsync(
        CreateQuestionnaireCommand command,
        ApplicationDbContext dbContext,
        CancellationToken ct
    )
    {
        // TODO: validate against current user access

        QuestionnaireEntity entity = new QuestionnaireFormToEntityMapper()
            .MapToEntity(command.Model);

        entity.CreatorId = command.CreatorId;

        dbContext.Questionnaires.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        return entity.Id;
    }
}