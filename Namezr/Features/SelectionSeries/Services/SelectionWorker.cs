using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Consumers.Services;
using Namezr.Features.Eligibility.Data;
using Namezr.Features.Eligibility.Services;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.SelectionSeries.Data;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.SelectionSeries.Services;

public interface ISelectionWorker
{
    Task Roll(
        long seriesId,
        bool allowRestarts,
        bool forceRecalculateEligibility,
        int numberOfEntriesToSelect,
        CancellationToken ct = default
    );
}

[AutoConstructor]
[RegisterSingleton]
public partial class SelectionWorker : ISelectionWorker
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IEligibilityService _eligibilityService;
    private readonly IClock _clock;
    // TODO

    public async Task Roll(
        long seriesId,
        bool allowRestarts,
        bool forceRecalculateEligibility,
        int numberOfEntriesToSelect, // TODO: slightly better name to indicate that only picks count
        CancellationToken ct = default
    )
    {
        Instant startTime = _clock.GetCurrentInstant();

        using var _ = Diagnostics.ActivitySource.StartActivity($"{nameof(SelectionWorker)}.{nameof(Roll)}");

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        SelectionSeriesEntity seriesEntity = await dbContext.SelectionSeries
            .AsTracking()
            .Include(s => s.UserData)
            .SingleAsync(s => s.Id == seriesId, cancellationToken: ct);

        EligibilityConfigurationEntity eligibilityConfiguration = await GetEligibilityConfiguration(seriesEntity, ct);

        // TODO

        Dictionary<Guid, EligibilityResult> userEligibilities = new();

        // 1) Fetch all candidates
        List<(SelectionCandidateEntity Candidate, Guid UserId)> candidates = await GetAllCandidates(seriesEntity, ct);

        // 1.2) Remove all ineligible users

        HashSet<Guid> ineligibleUserIds = new(candidates.Count);
        foreach ((var _, Guid userId) in candidates)
        {
            // GetUserEligibility caches the result internally
            if (!(await GetUserEligibility(userId)).Any)
            {
                ineligibleUserIds.Add(userId);
            }
        }

        candidates.RemoveAll(x => ineligibleUserIds.Contains(x.UserId));

        // A bit more cache
        Dictionary<Guid, Guid> userIdPerCandidateId = candidates
            .ToDictionary(x => x.UserId, x => x.Candidate.Id);

        // Prep for saving
        List<SelectionEntryEntity> batchEntities = new(numberOfEntriesToSelect);
        
        // 2) Get user eligibility, obeying forceRecalculateEligibility
        // do this only if the user is a candidate but store the result in case of multiple cycles

        // 3) Get users which were not selected in the current cycle
        int currentCycle = seriesEntity.CompleteCyclesCount;

        SelectionEntryPickedEntity[] existingSelections = await dbContext.SelectionEntries
            .OfType<SelectionEntryPickedEntity>()
            .Where(e => e.Batch.SeriesId == seriesEntity.Id && e.Cycle == currentCycle)
            .ToArrayAsync(ct);

        HashSet<Guid> usersSelectedInCurrentCycle = existingSelections
            .Select(e => userIdPerCandidateId[e.CandidateId])
            .ToHashSet();

        // 4) Biased random selection
        int selectedCount = 0;

        while (selectedCount < numberOfEntriesToSelect) // TODO: handle no candidates for the current cycle
        {
            (SelectionCandidateEntity Candidate, double Modifier)[] candidateModifiers = candidates
                .Where(x => !usersSelectedInCurrentCycle.Contains(x.UserId)) // TODO: this must account for the just selected ones
                .Select(x => (x.Candidate, (double)userEligibilities[x.UserId].Modifier))
                .ToArray();

            double totalModifier = candidateModifiers.Sum(x => x.Modifier);
            double selectedPoint = Random.Shared.NextDouble() * totalModifier;

            double runningModifierTally = 0;
            bool found = false;
            foreach ((SelectionCandidateEntity candidate, double modifier) in candidateModifiers)
            {
                runningModifierTally += modifier;
                if (runningModifierTally >= selectedPoint)
                {
                    batchEntities.Add(new SelectionEntryPickedEntity
                    {
                        BatchPosition = 0, // Will be set later
                        
                        Candidate = candidate,
                        Cycle = seriesEntity.CompleteCyclesCount,
                    });

                    selectedCount++;

                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new UnreachableException();
            }
        }

        // 5) If not enough and allowRestarts, start a new cycle and repeat from 3 until complete

        // 6) Store used eligibility into user data entries

        // 7) Save to DB

        for (var i = 0; i < batchEntities.Count; i++)
        {
            batchEntities[i].BatchPosition = i;
        }
        
        seriesEntity.CompletedSelectionMarker = Guid.NewGuid();
        dbContext.SelectionBatches.Add(new SelectionBatchEntity
        {
            Series = seriesEntity,

            RollStartedAt = startTime,
            RollCompletedAt = _clock.GetCurrentInstant(),

            Entries = batchEntities,
        });

        await dbContext.SaveChangesAsync(ct);

        return;

        async ValueTask<EligibilityResult> GetUserEligibility(Guid userId)
        {
            if (userEligibilities.TryGetValue(userId, out EligibilityResult? result))
            {
                return result;
            }

            result = await _eligibilityService.ClassifyEligibility(
                userId,
                eligibilityConfiguration,
                forceRecalculateEligibility ? UserStatusSyncEagerness.Force : UserStatusSyncEagerness.Default
            );
            userEligibilities[userId] = result;

            return result;
        }
    }

    private async Task<List<(SelectionCandidateEntity, Guid UserId)>> GetAllCandidates(
        SelectionSeriesEntity series,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        switch (series.OwnershipType)
        {
            case EligibilityConfigurationOwnershipType.Questionnaire:
            {
                QuestionnaireSubmissionEntity[] submissions = await dbContext.QuestionnaireSubmissions
                    .Where(s => s.Version.Questionnaire.Id == series.QuestionnaireId)
                    .ToArrayAsync(ct);

                return submissions
                    .Select(s => ((SelectionCandidateEntity)s, s.UserId))
                    .ToList();
            }

            default:
                throw new NotSupportedException(
                    $"Only {EligibilityConfigurationOwnershipType.Questionnaire} is implemented currently"
                );
        }
    }

    private async ValueTask<EligibilityConfigurationEntity> GetEligibilityConfiguration(
        SelectionSeriesEntity series,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        return await dbContext.EligibilityConfigurations
            .Include(e => e.Options)
            .SingleAsync(
                e =>
                    e.OwnershipType == series.OwnershipType &&
                    e.QuestionnaireId == series.QuestionnaireId,
                ct
            );
    }
}