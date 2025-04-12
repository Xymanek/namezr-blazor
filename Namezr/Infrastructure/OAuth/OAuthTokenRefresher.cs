using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using NodaTime;

namespace Namezr.Infrastructure.OAuth;

// ReSharper disable once UnusedTypeParameter - neeeds to be "passed" to the implementation
internal interface IOAuthTokenRefresher<TOptions>
    where TOptions : OAuthOptions
{
    Task<OAuthTokenPayload> RefreshToken(
        string authScheme,
        OAuthTokenPayload oldToken,
        CancellationToken cancellationToken
    );
}

[AutoConstructor]
internal partial class OAuthTokenRefresher<TOptions> : IOAuthTokenRefresher<TOptions>
    where TOptions : OAuthOptions
{
    private readonly IOptionsMonitor<TOptions> _options;
    private readonly IClock _clock;

    public async Task<OAuthTokenPayload> RefreshToken(
        string authScheme,
        OAuthTokenPayload oldToken,
        CancellationToken cancellationToken
    )
    {
        Guard.IsNotNull(oldToken.Data.RefreshToken);

        TOptions options = _options.Get(authScheme);

        // Based on
        // https://github.com/googleapis/google-api-dotnet-client/blob/439d25117d0a09d9746250b125a8b969ba0b9b74/Src/Support/Google.Apis.Auth.AspNetCore3/GoogleAuthProvider.cs#L80-L110

        FormUrlEncodedContent refreshContent = new(new Dictionary<string, string>
        {
            { "client_id", options.ClientId },
            { "client_secret", options.ClientSecret },
            { "grant_type", "refresh_token" },

            { "refresh_token", oldToken.Data.RefreshToken },
        });

        HttpResponseMessage refreshResponse = await options.Backchannel.PostAsync(
            options.TokenEndpoint,
            refreshContent,
            cancellationToken
        );
        Instant responseTime = _clock.GetCurrentInstant();

        refreshResponse.EnsureSuccessStatusCode();

        RefreshResponse? payload = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponse>(cancellationToken);
        Guard.IsNotNull(payload);

        OAuthTokenData tokenData = new(payload.AccessToken, payload.RefreshToken);
        OAuthTokenContext tokenContext = new()
        {
            ExpiresAt = responseTime
                .Plus(Duration.FromSeconds(payload.ExpiresIn)),
        };

        return new OAuthTokenPayload
        {
            Data = tokenData,
            Context = tokenContext,
        };
    }

    [UsedImplicitly]
    private class RefreshResponse
    {
        [JsonPropertyName("access_token")]
        public required string AccessToken { get; init; }

        [JsonPropertyName("refresh_token")]
        public required string RefreshToken { get; init; }

        [JsonPropertyName("expires_in")]
        public required int ExpiresIn { get; init; }

        [JsonPropertyName("scope")]
        public required string[] Scopes { get; init; }
    }
}