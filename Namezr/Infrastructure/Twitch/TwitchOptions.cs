using Namezr.Infrastructure.OAuth;

namespace Namezr.Infrastructure.Twitch;

public class TwitchOptions
{
    public const string SectionPath = "Twitch";
    
    public OAuthClientOptions OAuth { get; set; } = new();
    
    public string? MockServerUrl { get; set; }
}