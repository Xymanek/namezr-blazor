using System.Diagnostics;

namespace Namezr;

internal static class Diagnostics
{
    public static readonly string ActivitySourceName = "Namezr";
    
    internal static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}