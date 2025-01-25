﻿using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires;

[Handler]
[MapPost(ApiEndpointPaths.QuestionnairesNew)]
internal static partial class SaveQuestionnaireRequest
{
    private static async ValueTask<Guid> HandleAsync(
        // TODO: convert description to null if empty
        QuestionnaireEditModel model,
        ApplicationDbContext dbContext,
        CancellationToken ct
    )
    {
        QuestionnaireEntity entity = new QuestionnaireFormToEntityMapper()
            .MapToEntity(model);

        dbContext.Questionnaires.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        return entity.Id;
    }
}