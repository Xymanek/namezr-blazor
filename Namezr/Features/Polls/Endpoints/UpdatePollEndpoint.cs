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
[MapPost(ApiEndpointPaths.PollsUpdate)]
internal static partial class UpdatePollEndpoint
{
    private static async ValueTask HandleAsync(
        UpdatePollCommand request,
        IHttpContextAccessor httpContextAccessor,
        IdentityUserAccessor userAccessor,
        ApplicationDbContext dbContext,
        CancellationToken ct
    )
    {
        PollEntity? pollEntity = await dbContext.Polls
            .Include(x => x.Options)
            .Include(x => x.EligibilityConfiguration.Options)
            .AsSplitQuery()
            .AsTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (pollEntity is null)
        {
            // TODO: return 400
            throw new Exception("Poll not found");
        }

        await ValidateAccess();

        // TODO: updates the eligibility plan IDs to the same value since instances are not the same
        // since owned types are used
        request.Model.UpdateEntity(pollEntity);
        pollEntity.OptionsSetVersionMarker = Guid.NewGuid();

        await dbContext.SaveChangesAsync(ct);

        return;

        // TODO: unify with questionnaire
        async Task ValidateAccess()
        {
            ApplicationUser user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);

            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(staff =>
                    staff.UserId == user.Id &&
                    staff.CreatorId == pollEntity.CreatorId
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}