using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Models;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapGet("/api/canned-comments")]
internal static partial class GetCannedCommentsEndpoint
{
    private static async ValueTask<IResult> HandleAsync(
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext dbContext)
    {
        var user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);
        var creatorId = user.Id;
        var comments = await dbContext.CannedComments
            .Where(x => x.CreatorId == creatorId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CannedCommentResponseModel
            {
                Id = x.Id,
                Title = x.Title,
                Content = x.Content,
                CommentType = x.CommentType,
                Category = x.Category,
                CreatedAt = x.CreatedAt.ToDateTimeOffset(),
                UpdatedAt = x.UpdatedAt.ToDateTimeOffset(),
            })
            .ToListAsync();

        return Results.Ok(comments);
    }
}