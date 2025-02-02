using NodaTime;

namespace Namezr.Features.Twitch;

internal record TwitchTokenData(string AccessToken, string? RefreshToken);

internal record TwitchTokenContext
{
    public required Instant ExpiresAt { get; set; }
    
    // TODO: scopes
}

internal record TwitchTokenPayload
{
    public required TwitchTokenData Data { get; init; }
    public required TwitchTokenContext Context { get; init; }
}
