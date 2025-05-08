using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Behaviors] // Clear out the validation
[MapPost(ApiEndpointPaths.SubmissionLabelsPresenceMutate)]
internal partial class ApplySubmissionLabelEndpoint
{
    private static async ValueTask HandleAsync(
        MutateLabelPresenceRequest request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .AsTracking()
            .AsSplitQuery()
            .Include(submission => submission.Labels)
            .Include(submission => submission.Version.Questionnaire)
            .SingleOrDefaultAsync(submission => submission.Id == request.SubmissionId, ct);

        if (submission == null) throw new Exception("Bad submission ID");
        // TODO: validate access

        SubmissionLabelEntity? label = await dbContext.SubmissionLabels
            .AsTracking()
            .SingleOrDefaultAsync(label => label.Id == request.LabelId, ct);

        // TODO: 400 these
        if (label == null) throw new Exception("Bad label ID");
        if (label.CreatorId != submission.Version.Questionnaire.CreatorId)
        {
            throw new Exception("Label does not belong to same creator");
        }

        if (request.NewPresent)
        {
            submission.Labels!.Add(label);
        }
        else
        {
            submission.Labels!.Remove(label);
        }

        await dbContext.SaveChangesAsync(ct);
    }
}