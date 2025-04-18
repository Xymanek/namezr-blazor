﻿using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Features.Creators.Data;
using Namezr.Features.Files.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Creators.Endpoints;

[Handler]
[Behaviors] // Remove the global validation behavior
[MapGet(ApiEndpointPaths.SupportTargetsLogoDownload)]
internal partial class SupportTargetLogoEndpoint
{
    [UsedImplicitly]
    internal class Parameters
    {
        public required Guid SupportTargetId { get; init; }
    }

    private static async ValueTask<IResult> HandleAsync(
        [AsParameters] Parameters parameters,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IFileStorageService fileStorageService,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        SupportTargetEntity? supportTarget = await dbContext.SupportTargets
            .SingleOrDefaultAsync(x => x.Id == parameters.SupportTargetId, ct);

        // 404 if invalid support target ID or it does not have a logo
        if (supportTarget?.LogoFileId is null)
        {
            return Results.NotFound();
        }

        return Results.File(
            fileStorageService.GetFilePath(supportTarget.LogoFileId.Value),
            contentType: "image/webp"
        );
    }
}