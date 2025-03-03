using Microsoft.EntityFrameworkCore;
using Namezr.Features.Eligibility.Services;
using Namezr.Features.SelectionSeries.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.SelectionSeries.Services;

public interface ISelectionWorker
{
    Task Roll(
        long seriesId,
        bool allowRestarts,
        bool forceRecalculateEligibility,
        int numberOfEntriesToSelect
    );
}

[AutoConstructor]
[RegisterSingleton]
public partial class SelectionWorker : ISelectionWorker
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IEligibilityService _eligibilityService;
    // TODO

    public async Task Roll(
        long seriesId,
        bool allowRestarts,
        bool forceRecalculateEligibility,
        int numberOfEntriesToSelect
    )
    {
        // TODO: acquire distributed lock
        
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SelectionSeriesEntity seriesEntity = await dbContext.SelectionSeries
            .AsTracking()
            .Include(s => s.UserData)
            .SingleAsync(s => s.Id == seriesId);

        // TODO

        // 1) Fetch all candidates
        
        // 2) Get user eligibility, obeying forceRecalculateEligibility
        // do this only if the user is a candidate but store the result in case of multiple cycles
        
        // 3) Get users which were not selected in the current cycle
        
        // 4) Biased random selection
        
        // 5) If not enough and allowRestarts, start a new cycle and repeat from 3 until complete
        
        // 6) Store used eligibility into user data entries
        
        // 7) Save to DB
    }
}