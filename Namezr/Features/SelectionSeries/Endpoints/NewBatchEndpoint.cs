using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Selection;
using Namezr.Components.Account;
using Namezr.Features.Identity.Data;
using Namezr.Features.SelectionSeries.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.SelectionSeries.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.SelectionNewBatch)]
internal partial class NewBatchEndpoint
{
    private static async ValueTask Handle(
        NewSelectionBatchRequest request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IHttpContextAccessor httpContextAccessor,
        IdentityUserAccessor userAccessor,
        ISelectionWorker selectionWorker,
        CancellationToken ct
    )
    {
        await ValidateAccess();

        await selectionWorker.Roll(
            request.SeriesId,
            request.BatchOptions.AllowRestarts,
            request.BatchOptions.ForceRecalculateEligibility,
            request.BatchOptions.NumberOfEntriesToSelect,
            ct
        );

        async Task ValidateAccess()
        {
            ApplicationUser user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);

            await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            
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