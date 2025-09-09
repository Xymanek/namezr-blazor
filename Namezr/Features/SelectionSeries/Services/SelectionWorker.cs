using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Helpers;
using Namezr.Features.Consumers.Services;
using Namezr.Features.Eligibility.Data;
using Namezr.Features.Eligibility.Services;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.SelectionSeries.Data;
using Namezr.Infrastructure.Data;
using NodaTime;
using SuperLinq;

namespace Namezr.Features.SelectionSeries.Services;

public interface ISelectionWorker
{
    Task Roll(
        Guid seriesId,
        bool allowRestarts,
        bool forceRecalculateEligibility,
        int numberOfEntriesToSelect,
        List<Guid> includedLabelIds,
        List<Guid> excludedLabelIds,
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

    public async Task Roll(
        Guid seriesId,
        bool allowRestarts,
        bool forceRecalculateEligibility,
        int numberOfEntriesToSelect, // TODO: slightly better name to indicate that only picks count
        List<Guid> includedLabelIds,
        List<Guid> excludedLabelIds,
        CancellationToken ct = default
    )
    {
        using var _ = Diagnostics.ActivitySource.StartActivity($"{nameof(SelectionWorker)}.{nameof(Roll)}");

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        SelectionSeriesEntity seriesEntity = await dbContext.SelectionSeries
            .AsTracking()
            .Include(s => s.UserData)
            .SingleAsync(s => s.Id == seriesId, cancellationToken: ct);

        EligibilityConfigurationEntity eligibilityConfiguration = await GetEligibilityConfiguration(seriesEntity, ct);

        // Note: since the only place where GetAndCacheUserEligibility() is called
        // is inside foreach(candidates), this list also filters out the unapproved users.
        // Currently, this is convenient but technically misleading. 
        Dictionary<Guid, EligibilityResult> userEligibilities = new();

        // 1) Fetch all candidates
        List<(SelectionCandidateEntity Candidate, Guid UserId)> candidates =
            await GetAllCandidates(seriesEntity, includedLabelIds, excludedLabelIds, ct);

        // 1.2) Remove all ineligible users

        HashSet<Guid> ineligibleUserIds = new(candidates.Count);
        foreach ((var _, Guid userId) in candidates)
        {
            if (!(await GetAndCacheUserEligibility(userId)).Any)
            {
                ineligibleUserIds.Add(userId);
            }
        }

        candidates.RemoveAll(x => ineligibleUserIds.Contains(x.UserId));

        // A bit more cache
        Dictionary<Guid, Guid> userIdPerCandidateId = candidates
            .ToDictionary(x => x.Candidate.Id, x => x.UserId);

        // Prep for saving
        List<SelectionEntryEntity> batchEntities = new(numberOfEntriesToSelect);

        // 2) Get user eligibility, obeying forceRecalculateEligibility
        // do this only if the user is a candidate but store the result in case of multiple cycles

        // 3) Get users which were not selected in the current cycle
        int startingCycle = seriesEntity.CompleteCyclesCount;
        int currentCycle = startingCycle;

        SelectionEntryPickedEntity[] existingSelections = await dbContext.SelectionEntries
            .OfType<SelectionEntryPickedEntity>()
            // ReSharper disable once AccessToModifiedClosure - capture is unreferenced by the time the variable is modified
            .Where(e => e.Batch.SeriesId == seriesEntity.Id && e.Cycle == currentCycle)
            .ToArrayAsync(ct);

        // We might have selected candidates in the previous batch that are no longer in the candidate list,
        // e.g. if a submission had approval removed.
        // But still, cache the user IDs for these old candidates, else we will crash later.
        userIdPerCandidateId.AddRange(await GetUserIdForPotentialCandidateIds(
            seriesEntity,
            existingSelections
                .Select(x => x.CandidateId)
                .Where(x => !userIdPerCandidateId.ContainsKey(x)),
            ct
        ));

        // Will be updated below and by PickCurrentCycleMostPossible as we go,
        // but start with old values from DB 
        HashSet<Guid> usersSelectedInCurrentCycle = existingSelections
            .Select(e => userIdPerCandidateId[e.CandidateId])
            .ToHashSet();

        // 4) Biased random selection

        Instant rollStartTime = _clock.GetCurrentInstant();

        while (true)
        {
            int numberPicksBeforeCurrentCycle = CountPicksSoFar();
            PickCurrentCycleMostPossible();

            // We are done if we have fulfilled the request
            if (IsRequestFulfilled()) break;

            if (
                !allowRestarts ||

                // If we have not picked anything in the current cycle, we can't restart.
                // Otherwise, this will just endlessly loop.
                (
                    // Permit at least one restart else the process will never restart
                    // if there are no candidates left in the current/starting cycle
                    startingCycle != currentCycle &&
                    CountPicksSoFar() == numberPicksBeforeCurrentCycle
                )
            )
            {
                batchEntities.Add(new SelectionEntryEventEntity
                {
                    BatchPosition = 0, // Will be set later
                    Kind = SelectionEventKind.NoMoreCandidates,
                });
                break;
            }

            batchEntities.Add(new SelectionEntryEventEntity
            {
                BatchPosition = 0, // Will be set later
                Kind = SelectionEventKind.NewCycle,
            });

            currentCycle++;
            usersSelectedInCurrentCycle.Clear();
        }

        Instant rollCompletedAt = _clock.GetCurrentInstant();

        // 6) Store used eligibility into user data entries

        // Record users that have become ineligible
        seriesEntity.UserData!
            .Where(userData =>
                !userEligibilities.TryGetValue(userData.UserId, out EligibilityResult? userEligibility)
                || !userEligibility.Any
            )
            // TODO: this must be null; how to distinguish between ineligible and not approved?
            .ForEach(userData => userData.LatestModifier = 0);

        Dictionary<Guid, SelectionUserDataEntity> userDataPerUserId = seriesEntity.UserData!
            .ToDictionary(userData => userData.UserId);

        foreach ((Guid userId, EligibilityResult eligibility) in userEligibilities)
        {
            // Was cleared above
            if (!eligibility.Any) continue;

            if (userDataPerUserId.TryGetValue(userId, out SelectionUserDataEntity? userData))
            {
                userData.LatestModifier = eligibility.Modifier;
                // Counts will be updated below

                continue;
            }

            userData = new SelectionUserDataEntity
            {
                UserId = userId,

                LatestModifier = eligibility.Modifier,

                // Will be updated below
                SelectedCount = 0,
                TotalSelectedCount = 0,
            };

            userDataPerUserId[userId] = userData;
            seriesEntity.UserData!.Add(userData);
        }

        // Update whether the user has been selected in the current cycle or not.
        // Note: this handles the case where the user has been selected before but is now ineligible by
        // (1) user data entry would exist from the previous batch(es),
        //     so the skip in the previous loop is not a problem
        // (2) usersSelectedInCurrentCycle includes previous batch users, so the "selected in current cycle"
        //     status will be retained even if eligibility was lost between batches
        //     (but we are still in the same cycle as when the user was selected)
        foreach ((Guid userId, SelectionUserDataEntity userData) in userDataPerUserId)
        {
            userData.SelectedCount = usersSelectedInCurrentCycle.Contains(userId) ? 1 : 0;
        }

        // Update total selected counts by
        // adding how many times the user has been selected in the current batch to the previous total
        batchEntities
            .OfType<SelectionEntryPickedEntity>()
            .GroupBy(x => userIdPerCandidateId[x.CandidateId])
            .ForEach(grouping => userDataPerUserId[grouping.Key].TotalSelectedCount += grouping.Count());

        // 7) Save to DB

        for (var i = 0; i < batchEntities.Count; i++)
        {
            batchEntities[i].BatchPosition = i;
        }

        seriesEntity.CompletedSelectionMarker = Guid.NewGuid();
        seriesEntity.CompleteCyclesCount = currentCycle;

        dbContext.SelectionBatches.Add(new SelectionBatchEntity
        {
            Series = seriesEntity,

            RollStartedAt = rollStartTime,
            RollCompletedAt = rollCompletedAt,
            BatchType = SelectionBatchType.Automatic,

            Entries = batchEntities,
        });

        await dbContext.SaveChangesAsync(ct);

        return;

        async ValueTask<EligibilityResult> GetAndCacheUserEligibility(Guid userId)
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

        SelectionCandidateEntity? PickOneForCurrentCycle()
        {
            (SelectionCandidateEntity Candidate, double Modifier)[] candidateModifiers = candidates
                .Where(x => !usersSelectedInCurrentCycle.Contains(x.UserId))
                .Select(x => (x.Candidate, (double)userEligibilities[x.UserId].Modifier))
                .ToArray();

            if (candidateModifiers.Length == 0)
            {
                return null;
            }

            double totalModifier = candidateModifiers.Sum(x => x.Modifier);
            double selectedPoint = Random.Shared.NextDouble() * totalModifier;

            double runningModifierTally = 0;
            foreach ((SelectionCandidateEntity candidate, double modifier) in candidateModifiers)
            {
                runningModifierTally += modifier;
                if (runningModifierTally >= selectedPoint)
                {
                    return candidate;
                }
            }

            throw new UnreachableException();
        }

        void PickCurrentCycleMostPossible()
        {
            while (!IsRequestFulfilled())
            {
                SelectionCandidateEntity? candidate = PickOneForCurrentCycle();

                if (candidate == null)
                {
                    return;
                }

                usersSelectedInCurrentCycle.Add(userIdPerCandidateId[candidate.Id]);
                batchEntities.Add(new SelectionEntryPickedEntity
                {
                    BatchPosition = 0, // Will be set later

                    CandidateId = candidate.Id,
                    Cycle = currentCycle,
                });
            }
        }

        bool IsRequestFulfilled()
        {
            return numberOfEntriesToSelect <= CountPicksSoFar();
        }

        int CountPicksSoFar()
        {
            return batchEntities
                .OfType<SelectionEntryPickedEntity>()
                .Count();
        }
    }

    /// <remarks>
    /// Keep in sync with <see cref="GetUserIdForPotentialCandidateIds"/>.
    /// </remarks>
    private async Task<List<(SelectionCandidateEntity, Guid UserId)>> GetAllCandidates(
        SelectionSeriesEntity series,
        List<Guid> includedLabelIds,
        List<Guid> excludedLabelIds,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        switch (series.OwnershipType)
        {
            case EligibilityConfigurationOwnershipType.Questionnaire:
            {
                IQueryable<QuestionnaireSubmissionEntity> query = dbContext.QuestionnaireSubmissions
                    .Where(s => s.Version.Questionnaire.Id == series.QuestionnaireId)
                    .Where(s => s.ApprovedAt != null);

                if (includedLabelIds.Count > 0)
                {
                    query = query.Where(s => s.Labels!.Any(label => includedLabelIds.Contains(label.Id)));
                }

                if (excludedLabelIds.Count > 0)
                {
                    query = query.Where(s => s.Labels!.All(label => !excludedLabelIds.Contains(label.Id)));
                }

                QuestionnaireSubmissionEntity[] submissions = await query.ToArrayAsync(ct);

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

    /// <summary>
    /// Does not exclude currently invalid candidates (e.g. unapproved submission).
    /// </summary>
    /// <remarks>
    /// Keep in sync with <see cref="GetAllCandidates"/>.
    /// </remarks>
    private async Task<Dictionary<Guid, Guid>> GetUserIdForPotentialCandidateIds(
        SelectionSeriesEntity series,
        IEnumerable<Guid> candidateIds,
        CancellationToken ct
    )
    {
        candidateIds = candidateIds.ToArray();
        if (!candidateIds.Any()) return [];

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        switch (series.OwnershipType)
        {
            case EligibilityConfigurationOwnershipType.Questionnaire:
            {
                QuestionnaireSubmissionEntity[] submissions = await dbContext.QuestionnaireSubmissions
                    .Where(s => s.Version.Questionnaire.Id == series.QuestionnaireId)
                    .Where(submission => candidateIds.Contains(submission.Id))
                    .ToArrayAsync(ct);

                return submissions
                    .ToDictionary(x => x.Id, x => x.UserId);
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
            .AsNoTracking()
            .Include(e => e.Options)
            .SingleAsync(
                e =>
                    e.OwnershipType == series.OwnershipType &&
                    e.Questionnaire!.Id == series.QuestionnaireId,
                ct
            );
    }
}