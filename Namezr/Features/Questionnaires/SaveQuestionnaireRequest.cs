using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires;

[Handler]
[MapPost(ApiEndpointPaths.QuestionnairesSave)]
public static partial class SaveQuestionnaireRequest
{
    private static async ValueTask<Guid> HandleAsync(
        QuestionnaireEditModel model,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        CancellationToken ct
    )
    {
        // TODO: convert description to null if empty
        QuestionnaireEntity entity = model.MapToEntity();
        
        // TODO: UUIDv7
        // TODO: remove the need to manually do this
        entity.Id = QuestionnaireId.From(Guid.NewGuid());

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        dbContext.Questionnaires.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        return entity.Id.Value;
    }
}