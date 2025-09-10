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
[MapPost(ApiEndpointPaths.QuestionnairesNew)]
internal sealed partial class NewQuestionnaireEndpoint
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly ApplicationDbContext _dbContext;
    private async ValueTask<Guid> HandleAsync(
        CreateQuestionnaireCommand command,
        CancellationToken ct
    )
    {
        await ValidateAccess();

        QuestionnaireEntity entity = new QuestionnaireFormToEntityMapper()
            .MapToEntity(command.Model);

        entity.CreatorId = command.CreatorId;

        _dbContext.Questionnaires.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return entity.Id;
        
        async Task ValidateAccess()
        {
            ApplicationUser user = await _userAccessor.GetRequiredUserAsync(_httpContextAccessor.HttpContext!);

            bool isCreatorStaff = await _dbContext.CreatorStaff
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