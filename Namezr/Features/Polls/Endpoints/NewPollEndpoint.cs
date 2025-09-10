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
[AutoConstructor]
internal sealed partial class NewPollEndpoint
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly ApplicationDbContext _dbContext;

    private async ValueTask<Guid> HandleAsync(
        CreatePollCommand command,
        CancellationToken ct
    )
    {
        await ValidateAccess();

        PollEntity entity = command.Model.MapToEntity();
        entity.CreatorId = command.CreatorId;

        _dbContext.Polls.Add(entity);
        await _dbContext.SaveChangesAsync(ct);

        return entity.Id;
        
        // TODO: unify with questionnaire
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