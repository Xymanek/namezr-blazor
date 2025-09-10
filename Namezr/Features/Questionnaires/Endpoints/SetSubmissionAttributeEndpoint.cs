using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[MapPost(ApiEndpointPaths.SubmissionAttributesSet)]
[AutoConstructor]
internal sealed partial class SetSubmissionAttributeEndpoint
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IAttributeUpdaterService _attributeUpdater;

    private async ValueTask HandleAsync(
        SetSubmissionAttributeRequest request,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .AsTracking()
            .Include(submission => submission.Version.Questionnaire)
            .SingleOrDefaultAsync(submission => submission.Id == request.SubmissionId, ct);

        if (submission == null) throw new Exception("Bad submission ID");

        await _attributeUpdater.UpdateAttributeAsync(new AttributeUpdateCommand
        {
            InstigatorIsProgrammatic = false,

            SubmissionId = submission.Id,

            Key = request.Key,
            Value = request.Value,
        }, ct);

        return;

    }
}