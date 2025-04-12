using Namezr.Features.Files.Services;
using SkiaSharp;

namespace Namezr.Features.Files.Helpers;

public interface ILogoStorageHelper
{
    Task<Guid> MaybeResizeAndStoreLogo(Stream originalBitmapStream, CancellationToken ct);
}

[AutoConstructor]
[RegisterSingleton]
public partial class LogoStorageHelper : ILogoStorageHelper
{
    private readonly IFileStorageService _fileStorageService;

    private static readonly SKSizeI MaxSize = new(512, 512);
    private const int Quality = 80;

    public async Task<Guid> MaybeResizeAndStoreLogo(Stream originalBitmapStream, CancellationToken ct)
    {
        using SKBitmap original = SKBitmap.Decode(originalBitmapStream);

        if (original.Width > MaxSize.Width || original.Height > MaxSize.Height)
        {
            original.Resize(MaxSize, SKSamplingOptions.Default);
        }

        // TODO: resize the logo if it is not a square

        await using MemoryStream outputWebpStream = new();
        ct.ThrowIfCancellationRequested();

        original.Encode(outputWebpStream, SKEncodedImageFormat.Webp, Quality);
        outputWebpStream.Seek(0, SeekOrigin.Begin);

        return await _fileStorageService.StoreFile(outputWebpStream, ct);
    }
}