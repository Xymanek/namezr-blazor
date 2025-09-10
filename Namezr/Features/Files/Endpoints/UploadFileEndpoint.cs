using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Mvc;
using Namezr.Client;
using Namezr.Client.Types;
using Namezr.Features.Files.Services;

namespace Namezr.Features.Files.Endpoints;

[Handler]
[Behaviors] // Remove the global validation behavior
[MapPost(ApiEndpointPaths.FilesUpload)]
[AutoConstructor]
public sealed partial class UploadFileEndpoint
{
    internal static void CustomizeEndpoint(IEndpointConventionBuilder endpoint)
    {
        // TODO: is there a way?
        endpoint.DisableAntiforgery();
    }

    public sealed record Payload
    {
        public required IFormFile File { get; init; }

        [FromQuery]
        public required string Ticket { get; init; }
    }

    private readonly IFileUploadTicketHelper _ticketHelper;
    private readonly IFileStorageService _storageService;

    private async ValueTask<NewFileResult> Handle(
        [AsParameters] Payload payload,
        CancellationToken ct
    )
    {
        // TODO: catch exception and return 400 (+ return 400 in below cases)
        NewFileRestrictions restrictions = _ticketHelper.UnprotectRestrictionsForCurrentUser(payload.Ticket);

        if (restrictions.MinBytes != null && payload.File.Length < restrictions.MinBytes)
        {
            throw new Exception("File is too small");
        }

        if (restrictions.MaxBytes != null && payload.File.Length > restrictions.MaxBytes)
        {
            throw new Exception("File is too big");
        }

        if (restrictions.AllowedExtensions?.Count > 0)
        {
            bool foundMatch = false;
            foreach (string extension in restrictions.AllowedExtensions)
            {
                foundMatch = payload.File.FileName.EndsWith(
                    $".{extension}",
                    StringComparison.InvariantCultureIgnoreCase
                );

                if (foundMatch) break;
            }

            if (!foundMatch)
            {
                throw new Exception("File extension is not allowed");
            }
        }

        await using Stream readStream = payload.File.OpenReadStream();
        Guid fileId = await _storageService.StoreFile(readStream, ct);

        string ticket = _ticketHelper.CreateForCurrentUser(new UploadedFileInfo
        {
            FileId = fileId,
            LengthBytes = payload.File.Length,
            OriginalFileName = payload.File.FileName,
        });
        
        return new NewFileResult
        {
            FileId = fileId,
            Ticket = ticket,
        };
    }
}