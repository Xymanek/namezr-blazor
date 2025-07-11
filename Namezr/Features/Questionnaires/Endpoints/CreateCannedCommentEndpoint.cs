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
[MapPost("/api/canned-comments")]
internal static partial class CreateCannedCommentEndpoint
{
    private static async ValueTask<IResult> HandleAsync(
        CreateCannedCommentRequest request,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        ApplicationDbContext dbContext)
    {
        var user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);

        var now = SystemClock.Instance.GetCurrentInstant();

        var entity = new CannedCommentEntity
        {
            Id = Guid.NewGuid(),
            CreatorId = user.Id,
            Title = request.Title,
            Content = request.Content,
            CommentType = request.CommentType,
            Category = request.Category,
            CreatedAt = now,
            UpdatedAt = now,
        };

        dbContext.CannedComments.Add(entity);
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