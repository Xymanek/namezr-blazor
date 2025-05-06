using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.LabelsManagement;
using Namezr.Features.Creators.Data;
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
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        CreatorEntity creator = await dbContext.Creators
            .AsTracking()
            .SingleAsync(creator => creator.Id == request.CreatorId, ct);

        // TODO: validate not null and accessible to user

        SubmissionLabelEntity? labelEntity = null;
        if (request.Label.Id != Guid.Empty)
        {
            labelEntity = await dbContext.SubmissionLabels
                .AsTracking()
                .SingleOrDefaultAsync(label => label.Id == request.Label.Id, ct);
        }

        if (labelEntity != null)
        {
            // TODO: validate matching creator

            request.Label.ToEntity(labelEntity);
        }
        else
        {
            labelEntity = request.Label.ToEntity();
            labelEntity.Creator = creator;
        }

        dbContext.SubmissionLabels.Add(labelEntity);
        await dbContext.SaveChangesAsync(ct);
    }
}