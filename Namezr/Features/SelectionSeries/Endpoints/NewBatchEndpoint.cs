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
    private readonly ISelectionWorker _selectionWorker;

    private async ValueTask Handle(
        NewSelectionBatchRequest request,
        CancellationToken ct
    )
    {
        await _selectionWorker.Roll(
            request.SeriesId,
            request.BatchOptions.AllowRestarts,
            request.BatchOptions.ForceRecalculateEligibility,
            request.BatchOptions.NumberOfEntriesToSelect,
            request.BatchOptions.IncludedLabelIds,
            request.BatchOptions.ExcludedLabelIds,
            request.BatchOptions.RequiredAttributes,
            request.BatchOptions.ExcludedAttributes,
            ct
        );

    }
}