using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Selection;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Identity.Data;
using Namezr.Features.SelectionSeries.Data;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.SelectionSeries.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.SelectionManualAddEntries)]
[AutoConstructor]
internal sealed partial class ManualAddEntriesEndpoint
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IClock _clock;

    private async ValueTask<ManualAddEntriesResponse> Handle(
        ManualAddEntriesRequest request,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        // Get the selection series
        SelectionSeriesEntity series = await dbContext.SelectionSeries
            .AsTracking()
            .SingleAsync(s => s.Id == request.SeriesId, cancellationToken: ct);

        // Get all submissions to be added
        IEnumerable<Guid> requestSubmissionIds = request.SubmissionIds;
        QuestionnaireSubmissionEntity[] submissions = await dbContext.QuestionnaireSubmissions
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => requestSubmissionIds.Contains(s.Id))
            .ToArrayAsync(ct);

        // Check which submissions are already in the current cycle
        HashSet<Guid> existingSubmissionIds = await dbContext.SelectionEntries
            .OfType<SelectionEntryPickedEntity>()
            .Where(e => e.Batch.SeriesId == series.Id && e.Cycle == series.CompleteCyclesCount)
            .Select(e => e.CandidateId)
            .ToHashSetAsync(ct);

        List<SkippedSubmissionInfo> skippedSubmissions = new();
        List<SelectionEntryEntity> entriesToAdd = new();

        // Process each submission
        int batchPosition = 0;
        foreach (QuestionnaireSubmissionEntity submission in submissions)
        {
            if (existingSubmissionIds.Contains(submission.Id))
            {
                skippedSubmissions.Add(new SkippedSubmissionInfo
                {
                    SubmissionId = submission.Id,
                    SubmissionNumber = submission.Number,
                    UserDisplayName = submission.User.UserName!,
                    Reason = "Already exists in current cycle",
                });
                continue;
            }

            // Create a new selection entry
            entriesToAdd.Add(new SelectionEntryPickedEntity
            {
                CandidateId = submission.Id,
                Cycle = series.CompleteCyclesCount,
                BatchPosition = batchPosition++,
            });
        }

        // If we have entries to add, create a batch
        if (entriesToAdd.Count > 0)
        {
            Instant now = _clock.GetCurrentInstant();
            
            SelectionBatchEntity batch = new()
            {
                SeriesId = series.Id,
                RollStartedAt = now,
                RollCompletedAt = now,
                BatchType = SelectionBatchType.Manual,
                Entries = entriesToAdd,
            };

            // Set the batch reference for each entry
            foreach (SelectionEntryEntity entry in entriesToAdd)
            {
                entry.Batch = batch;
            }

            dbContext.SelectionBatches.Add(batch);
            series.CompletedSelectionMarker = Guid.NewGuid();

            await dbContext.SaveChangesAsync(ct);
        }

        return new ManualAddEntriesResponse
        {
            AddedCount = entriesToAdd.Count,
            SkippedSubmissions = skippedSubmissions.ToArray(),
        };

    }
}