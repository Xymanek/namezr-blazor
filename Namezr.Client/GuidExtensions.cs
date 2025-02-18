namespace Namezr.Client;

public static class GuidExtensions
{
    public static string NoHyphens(this Guid guid) => guid.ToString("N");
}