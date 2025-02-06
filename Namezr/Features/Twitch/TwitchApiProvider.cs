using System.Text.Json;
using AspNet.Security.OAuth.Twitch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Namezr.Features.Identity.Data;
using Namezr.Features.ThirdParty;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.OAuth;
using NodaTime;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Interfaces;

namespace Namezr.Features.Twitch;

public interface ITwitchApiProvider
{
    Task<ITwitchAPI> GetTwitchApiForUser(Guid userId);
}

[AutoConstructor]
[RegisterSingleton]
public partial class TwitchApiProvider : ITwitchApiProvider
{
    private readonly IOptionsMonitor<TwitchAuthenticationOptions> _twitchOptions;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<TwitchApiProvider> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IClock _clock;

    public async Task<ITwitchAPI> GetTwitchApiForUser(
        Guid userId /* TODO: enum to get either personal or creator token */
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .Include(x => x.ThirdPartyToken)
            .Where(x => x.UserId == userId && x.LoginProvider == TwitchAuthenticationDefaults.AuthenticationScheme)
            .SingleAsync();

        return await GetTwitchApi(userLogin.ThirdPartyToken!);
    }

    private async ValueTask<ITwitchAPI> GetTwitchApi(ThirdPartyToken token)
    {
        TwitchAuthenticationOptions twitchOptions = _twitchOptions
            .Get(TwitchAuthenticationDefaults.AuthenticationScheme);

        OAuthTokenData tokenData =
            token.Value.Deserialize<OAuthTokenData>()
            ?? throw new Exception("Deserialized token data is null??");

        OAuthTokenContext tokenContext =
            token.Context?.Deserialize<OAuthTokenContext>()
            ?? throw new Exception("Deserialized token context is null??");

        TwitchAPI twitchApi = new(_loggerFactory)
        {
            Settings =
            {
                ClientId = twitchOptions.ClientId,
                AccessToken = tokenData.AccessToken,
            }
        };

        bool mustRefresh = tokenContext.ExpiresAt >= _clock.GetCurrentInstant();

        if (!mustRefresh)
        {
            // TODO: should not be done more often than every 1 hour
            try
            {
                LogValidatingToken(token.Id, token.ServiceAccountId);

                ValidateAccessTokenResponse response = await twitchApi.Auth.ValidateAccessTokenAsync();

                if (response is null)
                {
                    throw new ValidateAccessTokenResponseNullException();
                }

                LogTokenValidationSuccessful(token.Id, token.ServiceAccountId);
            }
            catch (Exception e) when (e is HttpResponseException or ValidateAccessTokenResponseNullException)
            {
                LogTokenValidationFailed(e, token.Id, token.ServiceAccountId);
                mustRefresh = true;
            }
        }

        if (mustRefresh)
        {
            if (tokenData.RefreshToken is null)
            {
                throw new Exception("Attempting to refresh twitch token but no refresh token is stored");
            }

            LogRefreshingToken(token.Id, token.ServiceAccountId);

            // TODO: somehow gracefully handle this and instead inform the user that there is a problem with twitch connection
            // TODO: stampede protection
            RefreshResponse response = await twitchApi.Auth.RefreshAuthTokenAsync(
                tokenData.RefreshToken, twitchOptions.ClientSecret,
                // TODO: this is optional - should we use it?
                twitchOptions.ClientId
            );

            try
            {
                await StoreRefreshedToken(token, response);
            }
            catch (Exception e)
            {
                LogFailedToSaveRefreshedToken(e);
            }

            // Current usages should be with the new token.
            twitchApi.Settings.AccessToken = response.AccessToken;
        }

        return twitchApi;
    }

    [LoggerMessage(
        LogLevel.Trace,
        "Sending token validation request. " +
        "ThirdPartyToken ID: {tokenId}. " +
        "Twitch User ID: {twitchUserId}."
    )]
    private partial void LogValidatingToken(long tokenId, string twitchUserId);

    [LoggerMessage(
        LogLevel.Trace,
        "Token validation successful. " +
        "ThirdPartyToken ID: {tokenId}. " +
        "Twitch User ID: {twitchUserId}."
    )]
    private partial void LogTokenValidationSuccessful(long tokenId, string twitchUserId);

    private class ValidateAccessTokenResponseNullException() : Exception("ValidateAccessTokenResponse is null");

    [LoggerMessage(
        LogLevel.Trace,
        "Sending token refresh request. " +
        "ThirdPartyToken ID: {tokenId}. " +
        "Twitch User ID: {twitchUserId}."
    )]
    private partial void LogRefreshingToken(long tokenId, string twitchUserId);

    [LoggerMessage(
        LogLevel.Debug,
        "Token validation failed. " +
        "ThirdPartyToken ID: {tokenId}. " +
        "Twitch User ID: {twitchUserId}."
    )]
    private partial void LogTokenValidationFailed(Exception e, long tokenId, string twitchUserId);

    [LoggerMessage(
        LogLevel.Error,
        "Failed to save refreshed token to database. " +
        "Immediate Twitch API operations will work but subsequent usages will need to re-refresh the token."
    )]
    private partial void LogFailedToSaveRefreshedToken(Exception e);

    private async ValueTask StoreRefreshedToken(ThirdPartyToken oldEntity, RefreshResponse response)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ThirdPartyToken entity = await dbContext.ThirdPartyTokens
            .AsTracking()
            .SingleAsync(x => x.Id == oldEntity.Id);

        entity.Value =
            JsonSerializer.SerializeToDocument(new OAuthTokenData(response.AccessToken, response.RefreshToken));

        entity.Context = JsonSerializer.SerializeToDocument(new OAuthTokenContext
        {
            ExpiresAt = _clock.GetCurrentInstant()
                // TODO: is this seconds? Or something else?
                .Plus(Duration.FromSeconds(response.ExpiresIn)),
        });

        await dbContext.SaveChangesAsync();

        LogStoredRefreshedToken(oldEntity.Id, oldEntity.ServiceAccountId);
    }

    [LoggerMessage(
        LogLevel.Debug,
        "Successfully stored refreshed token. " +
        "ThirdPartyToken ID: {tokenId}. " +
        "Twitch User ID: {twitchUserId}."
    )]
    private partial void LogStoredRefreshedToken(long tokenId, string twitchUserId);
}