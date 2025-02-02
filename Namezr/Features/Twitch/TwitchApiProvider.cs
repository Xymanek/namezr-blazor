using System.Text.Json;
using AspNet.Security.OAuth.Twitch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Namezr.Features.Identity.Data;
using Namezr.Features.ThirdParty;
using Namezr.Infrastructure.Data;
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

        TwitchTokenData tokenData =
            token.Value.Deserialize<TwitchTokenData>()
            ?? throw new Exception("Deserialized token data is null??");

        TwitchTokenContext tokenContext =
            token.Context?.Deserialize<TwitchTokenContext>()
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
                ValidateAccessTokenResponse response = await twitchApi.Auth.ValidateAccessTokenAsync();
                
                if (response is null)
                {
                    throw new ValidateAccessTokenResponseNullException();
                }
            }
            catch (Exception e) when (e is HttpResponseException or ValidateAccessTokenResponseNullException)
            {
                LogTokenValidationFailed(e);
                mustRefresh = true;
            }
        }

        if (mustRefresh)
        {
            if (tokenData.RefreshToken is null)
            {
                throw new Exception("Attempting to refresh twitch token but no refresh token is stored");
            }

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

    private class ValidateAccessTokenResponseNullException() : Exception("ValidateAccessTokenResponse is null");

    [LoggerMessage(LogLevel.Debug, "Token validation failed")]
    private partial void LogTokenValidationFailed(Exception e);

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
            JsonSerializer.SerializeToDocument(new TwitchTokenData(response.AccessToken, response.RefreshToken));

        entity.Context = JsonSerializer.SerializeToDocument(new TwitchTokenContext
        {
            ExpiresAt = _clock.GetCurrentInstant()
                // TODO: is this seconds? Or something else?
                .Plus(Duration.FromSeconds(response.ExpiresIn)),
        });

        await dbContext.SaveChangesAsync();
    }
}