using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Selection;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Identity.Data;
using Namezr.Features.SelectionSeries.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.SelectionSeries.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.SelectionNewBatch)]
[AutoConstructor]
internal sealed partial class NewBatchEndpoint
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly ISelectionWorker _selectionWorker;

    private async ValueTask Handle(
        NewSelectionBatchRequest request,
        CancellationToken ct
    )
    {
        await ValidateAccess();

        await _selectionWorker.Roll(
            request.SeriesId,
            request.BatchOptions.AllowRestarts,
            request.BatchOptions.ForceRecalculateEligibility,
            request.BatchOptions.NumberOfEntriesToSelect,
            request.BatchOptions.IncludedLabelIds,
            request.BatchOptions.ExcludedLabelIds,
            ct
        );

        async Task ValidateAccess()
        {
            ApplicationUser user = await _userAccessor.GetRequiredUserAsync(_httpContextAccessor.HttpContext!);

            await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
            
            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(
                    staff =>
                        staff.UserId == user.Id &&
                        staff.Creator.Questionnaires!
                            .Any(ques => ques.SelectionSeries!.Any(series => series.Id == request.SeriesId))
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}