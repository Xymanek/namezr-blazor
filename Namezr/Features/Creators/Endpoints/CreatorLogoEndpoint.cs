using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Features.Creators.Data;
using Namezr.Features.Files.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Creators.Endpoints;

[Handler]
[Behaviors] // Remove the global validation behavior
[MapGet(ApiEndpointPaths.CreatorsLogoDownload)]
internal partial class CreatorLogoEndpoint
{
    internal class Parameters
    {
        public required Guid CreatorId { get; init; }
    }

    private static async ValueTask<IResult> HandleAsync(
        [AsParameters] Parameters parameters,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IFileStorageService fileStorageService,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        CreatorEntity? creator = await dbContext.Creators
            .SingleOrDefaultAsync(x => x.Id == parameters.CreatorId, ct);

        if (creator is null)
        {
            return Results.NotFound();
        }

        // TODO: fixed placeholder if no logo
        if (creator.LogoFileId is null)
        {
            return Results.NotFound();
        }

        return Results.File(
            fileStorageService.GetFilePath(creator.LogoFileId.Value),
            contentType: "image/webp"
        );
    }
}