namespace Namezr.Helpers;

internal static class PatreonHelpers
{
    public static string GetFullPatreonUrl(string maybeSubpath)
    {
        if (maybeSubpath.StartsWith("https://www.patreon.com/"))
        {
            return maybeSubpath;
        }

        UriBuilder builder = new("https", "www.patreon.com")
        {
            Path = maybeSubpath
        };

        return builder.Uri.ToString();
    }
}