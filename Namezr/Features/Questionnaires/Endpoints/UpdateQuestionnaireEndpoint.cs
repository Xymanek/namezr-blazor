using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Identity.Data;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[AutoConstructor]
[Authorize]
[MapPost(ApiEndpointPaths.QuestionnairesUpdate)]
internal sealed partial class UpdateQuestionnaireEndpoint
{
    private readonly ApplicationDbContext _dbContext;

    private async ValueTask HandleAsync(
        UpdateQuestionnaireCommand request,
        CancellationToken ct
    )
    {
        QuestionnaireEntity? questionnaireEntity = await _dbContext.Questionnaires
            .Include(x => x.Versions)
            .Include(x => x.Fields)
            .Include(x => x.EligibilityConfiguration).ThenInclude(x => x.Options)
            .AsSplitQuery()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.QuestionnaireId, ct);

        if (questionnaireEntity is null)
        {
            // TODO: return 400
            throw new Exception("Questionnaire not found");
        }

        // TODO: updates the eligibility plan IDs to the same value since instances are not the same
        // since owned types are used
        new QuestionnaireFormToEntityMapper(questionnaireEntity.Fields!)
            .UpdateEntityWithNewVersion(request.Model, questionnaireEntity);

        await _dbContext.SaveChangesAsync(ct);

        return;

    }
}