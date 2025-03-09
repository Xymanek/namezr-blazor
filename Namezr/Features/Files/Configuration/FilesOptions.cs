namespace Namezr.Features.Files.Configuration;

public class FilesOptions
{
    internal const string SectionPath = "App:Files";

    public string StoragePath { get; set; } = "FileStorage";
}