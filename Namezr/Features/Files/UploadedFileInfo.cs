namespace Namezr.Features.Files;

public record UploadedFileInfo
{
    public required Guid FileId { get; init; }
    public required long LengthBytes { get; init; }
    public required string OriginalFileName { get; init; }

    // TODO: replace with some kind of session ID
    public string? UserId { get; init; }
};