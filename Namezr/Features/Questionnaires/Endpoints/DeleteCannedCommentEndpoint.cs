using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapDelete("/api/canned-comments/{id:guid}")]
internal static partial class DeleteCannedCommentEndpoint
{
    private static async ValueTask<IResult> HandleAsync(
        Guid id,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext dbContext)
    {
        var user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);

        var entity = await dbContext.CannedComments
            .Where(x => x.Id == id && x.CreatorId == user.Id)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            return Results.NotFound();
        }

        dbContext.CannedComments.Remove(entity);
        await dbContext.SaveChangesAsync();

        return Results.NoContent();
    }
}