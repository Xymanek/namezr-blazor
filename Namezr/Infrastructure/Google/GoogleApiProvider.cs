using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.ThirdParty.Data;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.OAuth;
using NodaTime;

namespace Namezr.Infrastructure.Google;

internal interface IGoogleApiProvider
{
    Task<BaseClientService.Initializer> GetInitializer(ThirdPartyToken token, CancellationToken ct);
}

[AutoConstructor]
internal partial class GoogleApiProvider : IGoogleApiProvider
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IOAuthTokenRefresher<GoogleOptions> _tokenRefresher;
    private readonly ILogger<GoogleApiProvider> _logger;
    private readonly IClock _clock;

    public async Task<BaseClientService.Initializer> GetInitializer(ThirdPartyToken token, CancellationToken ct)
    {
        GoogleCredential credential = await GetCredential(token, ct);

        return new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
        };
    }

    private async Task<GoogleCredential> GetCredential(ThirdPartyToken token, CancellationToken ct)
    {
        OAuthTokenData tokenData =
            token.Value.Deserialize<OAuthTokenData>()
            ?? throw new Exception("Deserialized token data is null??");

        OAuthTokenContext tokenContext =
            token.Context?.Deserialize<OAuthTokenContext>()
            ?? throw new Exception("Deserialized token context is null??");

        bool mustRefresh = tokenContext.ExpiresAt >= _clock.GetCurrentInstant();

        if (mustRefresh)
        {
            LogRefreshingToken(token.Id, token.ServiceAccountId);

            // TODO: somehow gracefully handle this and instead inform the user that there is a problem with google connection
            // TODO: stampede protection

            OAuthTokenPayload newToken = await _tokenRefresher.RefreshToken(
                GoogleDefaults.AuthenticationScheme,
                new OAuthTokenPayload(tokenData, tokenContext),
                ct
            );

            try
            {
                await StoreRefreshedToken(token, newToken);
            }
            catch (Exception e)
            {
                LogFailedToSaveRefreshedToken(e);
            }

            return GoogleCredential.FromAccessToken(newToken.Data.AccessToken);
        }

        return GoogleCredential.FromAccessToken(tokenData.AccessToken);
    }

    [LoggerMessage(
        LogLevel.Trace,
        "Sending token refresh request. " +
        "ThirdPartyToken ID: {tokenId}. " +
        "Google User ID: {googleUserId}."
    )]
    private partial void LogRefreshingToken(long tokenId, string googleUserId);

    [LoggerMessage(
        LogLevel.Error,
        "Failed to save refreshed token to database. " +
        "Immediate Google API operations will work but subsequent usages will need to re-refresh the token."
    )]
    private partial void LogFailedToSaveRefreshedToken(Exception e);

    private async ValueTask StoreRefreshedToken(ThirdPartyToken oldEntity, OAuthTokenPayload payload)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ThirdPartyToken entity = await dbContext.ThirdPartyTokens
            .AsTracking()
            .SingleAsync(x => x.Id == oldEntity.Id);

        entity.Value = JsonSerializer.SerializeToDocument(payload.Data);
        entity.Context = JsonSerializer.SerializeToDocument(payload.Context);

        await dbContext.SaveChangesAsync();

        LogStoredRefreshedToken(oldEntity.Id, oldEntity.ServiceAccountId);
    }

    [LoggerMessage(
        LogLevel.Debug,
        "Successfully stored refreshed token. " +
        "ThirdPartyToken ID: {tokenId}. " +
        "Twitch User ID: {googleUserId}."
    )]
    private partial void LogStoredRefreshedToken(long tokenId, string googleUserId);
}