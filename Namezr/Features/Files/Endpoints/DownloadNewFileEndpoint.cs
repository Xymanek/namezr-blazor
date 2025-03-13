using System.Diagnostics.CodeAnalysis;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Mvc;
using Namezr.Client;
using Namezr.Features.Files.Services;

namespace Namezr.Features.Files.Endpoints;

[Handler]
[Behaviors] // Remove the global validation behavior
[MapGet(ApiEndpointPaths.FilesDownloadNew)]
public partial class DownloadNewFileEndpoint
{
    public sealed record Payload
    {
        [FromQuery]
        public required string Ticket { get; init; }
    }


    [SuppressMessage("ImmediateHandler", "IHR0012:Handler method should use CancellationToken")]
    private static ValueTask<IResult> Handle(
        [AsParameters] Payload payload,
        IFileUploadTicketHelper ticketHelper,
        IFileStorageService storageService
    )
    {
        UploadedFileInfo fileInfo = ticketHelper.UnprotectUploadedForCurrentUser(payload.Ticket);

        return ValueTask.FromResult(Results.File(
            storageService.GetFilePath(fileInfo.FileId),
            fileDownloadName: fileInfo.OriginalFileName
        ));
    }
}