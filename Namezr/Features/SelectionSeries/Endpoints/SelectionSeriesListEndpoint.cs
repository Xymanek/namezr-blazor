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
internal partial class SelectionSeriesListEndpoint
{
    private static async ValueTask<SelectionSeriesModel[]> Handle(
        SelectionSeriesListRequest request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IHttpContextAccessor httpContextAccessor,
        IdentityUserAccessor userAccessor,
        CancellationToken ct
    )
    {
        await ValidateAccess();

        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

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

        async Task ValidateAccess()
        {
            ApplicationUser user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);

            await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);
            
            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(
                    staff =>
                        staff.UserId == user.Id &&
                        staff.Creator.Questionnaires!.Any(ques => ques.Id == request.QuestionnaireId)
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}