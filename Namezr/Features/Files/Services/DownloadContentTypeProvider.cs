using Microsoft.AspNetCore.StaticFiles;

namespace Namezr.Features.Files.Services;

public interface IDownloadContentTypeProvider
{
    string? MaybeGetFromFilename(string fileName);
}

[RegisterSingleton]
internal class DownloadContentTypeProvider : IDownloadContentTypeProvider
{
    private static readonly FileExtensionContentTypeProvider FileTypeProvider = new();

    public string? MaybeGetFromFilename(string fileName)
    {
        return FileTypeProvider.TryGetContentType(fileName, out string? contentType)
            ? contentType
            : null;
    }
}