using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.LabelsManagement;
using Namezr.Features.Creators.Data;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Mappers;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[MapPost(ApiEndpointPaths.SubmissionLabelsConfigSave)]
internal partial class SaveLabelConfigEndpoint
{
    private static async ValueTask HandleAsync(
        LabelSaveRequest request,
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

        SubmissionLabelEntity? labelEntity = null;
        if (request.Label.Id != Guid.Empty)
        {
            labelEntity = await dbContext.SubmissionLabels
                .AsTracking()
                .SingleOrDefaultAsync(label => label.Id == request.Label.Id, ct);
        }

        if (labelEntity != null)
        {
            if (labelEntity.CreatorId != creator.Id)
            {
                throw new Exception("Label does not belong to same creator"); // TODO: 400
            }

            request.Label.ToEntity(labelEntity);
        }
        else
        {
            labelEntity = request.Label.ToEntity();
            labelEntity.Creator = creator;

            dbContext.SubmissionLabels.Add(labelEntity);
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