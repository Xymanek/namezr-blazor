using System.Text.Json;
using AspNet.Security.OAuth.Twitch;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;
using Namezr.Features.ThirdParty;
using Namezr.Infrastructure.Auth;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Twitch;

[AutoConstructor]
[RegisterSingleton]
public partial class TwitchLoginHandler : ILoginProviderHandler
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<TwitchLoginHandler> _logger;

    public string LoginProvider => TwitchAuthenticationDefaults.AuthenticationScheme;

    public async ValueTask OnSignInAsync(ExternalSignInInfo signInInfo)
    {
        // TODO: how to wrap this in a transaction when creating a new user?
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .AsTracking()
            .Include(x => x.ThirdPartyToken)
            .SingleAsync(x => x.UserId == signInInfo.User.Id && x.LoginProvider == LoginProvider);

        TwitchTokenPayload tokenPayload = GetTokenPayload(signInInfo);

        JsonDocument value = JsonSerializer.SerializeToDocument(tokenPayload.Data);
        JsonDocument context = JsonSerializer.SerializeToDocument(tokenPayload.Context);

        if (userLogin.ThirdPartyToken == null)
        {
            LogCreatingThirdPartyToken(userLogin.UserId, signInInfo.ExternalLoginInfo.ProviderKey);

            userLogin.ThirdPartyToken = new ThirdPartyToken
            {
                ServiceType = TwitchConstants.ServiceType,
                ServiceAccountId = signInInfo.ExternalLoginInfo.ProviderKey,
                TokenType = ThirdPartyToken.DefaultTokenType,
                Value = value,
                Context = context,
            };
        }
        else
        {
            // TODO: validate that we are not reducing the scope

            LogUpdatingThirdPartyToken(userLogin.UserId, signInInfo.ExternalLoginInfo.ProviderKey);

            userLogin.ThirdPartyToken.Value = value;
            userLogin.ThirdPartyToken.Context = context;
        }

        await dbContext.SaveChangesAsync();
    }

    [LoggerMessage(
        LogLevel.Debug,
        "Creating third party token for user {userId} with Twitch ID {twitchId}"
    )]
    private partial void LogCreatingThirdPartyToken(Guid userId, string twitchId);

    [LoggerMessage(
        LogLevel.Debug,
        "Updating third party token for user {userId} with Twitch ID {twitchId}"
    )]
    private partial void LogUpdatingThirdPartyToken(Guid userId, string twitchId);

    private static TwitchTokenPayload GetTokenPayload(ExternalSignInInfo signInInfo)
    {
        IEnumerable<AuthenticationToken> tokens =
            signInInfo.ExternalLoginInfo.AuthenticationTokens ??
            throw new InvalidOperationException("Missing AuthenticationTokens when signing in with Twitch");

        Dictionary<string, string> tokensByName = tokens
            // TODO: const (and below)
            .Where(token => token.Name is "access_token" or "refresh_token" or "expires_at")
            .ToDictionary(token => token.Name, token => token.Value);

        if (!tokensByName.TryGetValue("access_token", out string? accessToken))
        {
            throw new InvalidOperationException("Missing access_token when signing in with Twitch");
        }

        if (!tokensByName.TryGetValue("expires_at", out string? expiresAt))
        {
            throw new InvalidOperationException("Missing expires_at when signing in with Twitch");
        }

        return new TwitchTokenPayload
        {
            Data = new TwitchTokenData(accessToken, tokensByName.GetValueOrDefault("refresh_token")),
            Context = new TwitchTokenContext
            {
                ExpiresAt = Instant.FromDateTimeOffset(DateTimeOffset.Parse(expiresAt)),
            },
        };
    }
}