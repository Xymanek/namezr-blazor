using Microsoft.Extensions.Options;
using Namezr.Features.Files.Configuration;

namespace Namezr.Features.Files.Services;

public interface IFileStorageService
{
    ValueTask<Guid> StoreFile(Stream stream, CancellationToken ct = default);
    string GetFilePath(Guid fileId);
}

[AutoConstructor]
[RegisterSingleton]
public partial class FileStorageService : IFileStorageService
{
    private readonly IOptionsMonitor<FilesOptions> _options;

    public async ValueTask<Guid> StoreFile(Stream stream, CancellationToken ct = default)
    {
        Guid fileId = Guid.NewGuid();

        string filePath = GetFilePath(fileId);

        await using FileStream fileStream = new(filePath, FileMode.CreateNew, FileAccess.Write);
        await stream.CopyToAsync(fileStream, ct);

        return fileId;
    }

    public string GetFilePath(Guid fileId)
    {
        string storageDir = Path.Combine(Environment.CurrentDirectory, _options.CurrentValue.StoragePath);

        // Ensure the directory exists
        Directory.CreateDirectory(storageDir);

        return Path.Combine(
            storageDir,
            fileId.ToString("D") // Force lower case
        );
    }
}

public static class FileStorageServiceExtensions
{
    public static FileStream OpenRead(this IFileStorageService service, Guid fileId)
    {
        return File.OpenRead(service.GetFilePath(fileId));
    }
}