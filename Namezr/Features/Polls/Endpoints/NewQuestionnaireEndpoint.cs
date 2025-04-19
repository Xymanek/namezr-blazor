using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Polls.Edit;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Identity.Data;
using Namezr.Features.Polls.Data;
using Namezr.Features.Polls.Mappers;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Polls.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.PollsNew)]
internal static partial class NewPollEndpoint
{
    private static async ValueTask<Guid> HandleAsync(
        CreatePollCommand command,
        IHttpContextAccessor httpContextAccessor,
        IdentityUserAccessor userAccessor,
        ApplicationDbContext dbContext,
        CancellationToken ct
    )
    {
        await ValidateAccess();

        PollEntity entity = command.Model.MapToEntity();
        entity.CreatorId = command.CreatorId;

        dbContext.Polls.Add(entity);
        await dbContext.SaveChangesAsync(ct);

        return entity.Id;
        
        // TODO: unify with questionnaire
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