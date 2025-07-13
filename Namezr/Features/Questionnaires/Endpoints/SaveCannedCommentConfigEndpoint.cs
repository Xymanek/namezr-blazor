using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.CannedCommentsManagement;
using Namezr.Features.Creators.Data;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Mappers;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[MapPost(ApiEndpointPaths.CannedCommentsConfigSave)]
internal partial class SaveCannedCommentConfigEndpoint
{
    private static async ValueTask HandleAsync(
        CannedCommentSaveRequest request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        CreatorEntity creator = await dbContext.Creators
            .AsTracking()
            .SingleAsync(creator => creator.Id == request.CreatorId, ct);

        // TODO: validate not null
        await ValidateAccess();

        CannedCommentEntity? cannedCommentEntity = null;
        if (request.CannedComment.Id != Guid.Empty)
        {
            cannedCommentEntity = await dbContext.CannedComments
                .AsTracking()
                .SingleOrDefaultAsync(comment => comment.Id == request.CannedComment.Id, ct);
        }

        if (cannedCommentEntity != null)
        {
            if (cannedCommentEntity.CreatorId != creator.Id)
            {
                throw new Exception("Canned comment does not belong to same creator"); // TODO: 400
            }

            request.CannedComment.ToEntity(cannedCommentEntity);
        }
        else
        {
            cannedCommentEntity = request.CannedComment.ToEntity();
            cannedCommentEntity.Creator = creator;

            dbContext.CannedComments.Add(cannedCommentEntity);
        }

        await dbContext.SaveChangesAsync(ct);
        return;

        async Task ValidateAccess()
        {
            Guid userId = userAccessor.GetRequiredUserId(httpContextAccessor.HttpContext!);

            // ReSharper disable once AccessToDisposedClosure
            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(staff =>
                    staff.UserId == userId &&
                    staff.CreatorId == request.CreatorId
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}