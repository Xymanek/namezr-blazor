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
[AutoConstructor]
[MapPost(ApiEndpointPaths.SubmissionLabelsConfigSave)]
internal sealed partial class SaveLabelConfigEndpoint
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    private async ValueTask HandleAsync(
        LabelSaveRequest request,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        CreatorEntity creator = await dbContext.Creators
            .AsTracking()
            .SingleAsync(creator => creator.Id == request.CreatorId, ct);

        // TODO: validate not null

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

    }
}