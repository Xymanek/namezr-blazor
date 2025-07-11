using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Models;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapPut("/api/canned-comments")]
internal static partial class UpdateCannedCommentEndpoint
{
    private static async ValueTask<IResult> HandleAsync(
        UpdateCannedCommentRequest request,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext dbContext)
    {
        var user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);

        var entity = await dbContext.CannedComments
            .Where(x => x.Id == request.Id && x.CreatorId == user.Id)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            return Results.NotFound();
        }

        entity.Title = request.Title;
        entity.Content = request.Content;
        entity.CommentType = request.CommentType;
        entity.Category = request.Category;
        entity.UpdatedAt = SystemClock.Instance.GetCurrentInstant();

        await dbContext.SaveChangesAsync();

        var response = new CannedCommentResponseModel
        {
            Id = entity.Id,
            Title = entity.Title,
            Content = entity.Content,
            CommentType = entity.CommentType,
            Category = entity.Category,
            CreatedAt = entity.CreatedAt.ToDateTimeOffset(),
            UpdatedAt = entity.UpdatedAt.ToDateTimeOffset(),
        };

        return Results.Ok(response);
    }
}