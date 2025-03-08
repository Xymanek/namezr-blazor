using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Selection;
using Namezr.Features.SelectionSeries.Services;

namespace Namezr.Features.SelectionSeries.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.SelectionNewBatch)]
public partial class NewBatchEndpoint
{
    private static async ValueTask Handle(
        NewSelectionBatchRequest request,
        ISelectionWorker selectionWorker,
        CancellationToken ct
    )
    {
        // TODO: validate access to the series
        
        await selectionWorker.Roll(
            request.SeriesId,
            request.BatchOptions.AllowRestarts,
            request.BatchOptions.ForceRecalculateEligibility,
            request.BatchOptions.NumberOfEntriesToSelect, 
            ct
        );
    }
}