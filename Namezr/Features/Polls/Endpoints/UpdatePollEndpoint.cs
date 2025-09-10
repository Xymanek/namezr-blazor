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
[AutoConstructor]
[Authorize]
[MapPost(ApiEndpointPaths.PollsUpdate)]
internal sealed partial class UpdatePollEndpoint
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly ApplicationDbContext _dbContext;

    private async ValueTask HandleAsync(
        UpdatePollCommand request,
        CancellationToken ct
    )
    {
        PollEntity? pollEntity = await _dbContext.Polls
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

        await _dbContext.SaveChangesAsync(ct);

        return;

        // TODO: unify with questionnaire
        async Task ValidateAccess()
        {
            ApplicationUser user = await _userAccessor.GetRequiredUserAsync(_httpContextAccessor.HttpContext!);

            bool isCreatorStaff = await _dbContext.CreatorStaff
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