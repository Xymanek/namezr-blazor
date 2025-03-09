using System.Collections.Immutable;

namespace Namezr.Features.Files;

public record NewFileRestrictions
{
    public long? MinBytes { get; init; }
    public long? MaxBytes { get; init; }

    public ImmutableList<string>? AllowedExtensions { get; init; }

    // TODO: replace with some kind of session ID
    public string? UserId { get; init; }
}