using System.Diagnostics.CodeAnalysis;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Mvc;
using Namezr.Client;
using Namezr.Features.Files.Services;

namespace Namezr.Features.Files.Endpoints;

[Handler]
[AutoConstructor]
[Behaviors] // Remove the global validation behavior
[MapGet(ApiEndpointPaths.FilesDownloadNew)]
public sealed partial class DownloadNewFileEndpoint
{
    private readonly IFileUploadTicketHelper _ticketHelper;
    private readonly IFileStorageService _storageService;
    private readonly IDownloadContentTypeProvider _contentTypeProvider;

    public sealed record Payload
    {
        [FromQuery]
        public required string Ticket { get; init; }
    }

    [SuppressMessage("ImmediateHandler", "IHR0012:Handler method should use CancellationToken")]
    private ValueTask<IResult> Handle(
        [AsParameters] Payload payload
    )
    {
        UploadedFileInfo fileInfo = _ticketHelper.UnprotectUploadedForCurrentUser(payload.Ticket);

        return ValueTask.FromResult(Results.File(
            _storageService.GetFilePath(fileInfo.FileId),
            contentType: _contentTypeProvider.MaybeGetFromFilename(fileInfo.OriginalFileName),
            fileDownloadName: fileInfo.OriginalFileName
        ));
    }
}