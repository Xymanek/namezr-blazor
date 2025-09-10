using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Selection;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.SelectionSeries.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.SelectionSeriesList)]
[AutoConstructor]
internal sealed partial class SelectionSeriesListEndpoint
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    private async ValueTask<SelectionSeriesModel[]> Handle(
        SelectionSeriesListRequest request,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        SelectionSeriesModel[] series = await dbContext.SelectionSeries
            .AsNoTracking()
            .Where(s => s.QuestionnaireId == request.QuestionnaireId)
            .Select(s => new SelectionSeriesModel
            {
                Id = s.Id,
                Name = s.Name,
            })
            .ToArrayAsync(ct);

        return series;

    }
}