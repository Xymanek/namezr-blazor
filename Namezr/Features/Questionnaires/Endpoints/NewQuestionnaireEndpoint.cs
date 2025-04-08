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
[Authorize]
[MapPost(ApiEndpointPaths.QuestionnairesNew)]
internal static partial class NewQuestionnaireEndpoint
{
    private static async ValueTask<Guid> HandleAsync(
        CreateQuestionnaireCommand command,
        IHttpContextAccessor httpContextAccessor,
        IdentityUserAccessor userAccessor,
        ApplicationDbContext dbContext,
        CancellationToken ct
    )
    {
        await ValidateAccess();

        QuestionnaireEntity entity = new QuestionnaireFormToEntityMapper()
            .MapToEntity(command.Model);

        entity.CreatorId = command.CreatorId;

        dbContext.Questionnaires.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        return entity.Id;
        
        async Task ValidateAccess()
        {
            ApplicationUser user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);

            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(
                    staff =>
                        staff.UserId == user.Id &&
                        staff.CreatorId == command.CreatorId
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}