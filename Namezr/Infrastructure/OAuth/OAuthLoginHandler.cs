using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;
using Namezr.Features.ThirdParty;
using Namezr.Infrastructure.Auth;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Infrastructure.OAuth;

[AutoConstructor]
internal abstract partial class OAuthLoginHandler : ILoginProviderHandler
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<OAuthLoginHandler> _logger;

    /// <inheritdoc />
    public abstract string LoginProvider { get; }
    
    /// <summary>
    /// Value for <see cref="P:Namezr.Features.ThirdParty.ThirdPartyToken.ServiceType"/>
    /// </summary>
    protected abstract string ServiceType { get; }

    /// <inheritdoc />
    public async ValueTask OnSignIn(ExternalSignInInfo signInInfo)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .AsTracking()
            .Include(x => x.ThirdPartyToken)
            .SingleAsync(x => x.UserId == signInInfo.User.Id && x.LoginProvider == LoginProvider);

        AugmentUserLogin(signInInfo, userLogin);

        await dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public ValueTask OnAssociate(ApplicationUserLogin userLogin, ExternalSignInInfo signInInfo)
    {
        AugmentUserLogin(signInInfo, userLogin);
        return ValueTask.CompletedTask;
    }

    private void AugmentUserLogin(ExternalSignInInfo signInInfo, ApplicationUserLogin userLogin)
    {
        OAuthTokenPayload tokenPayload = GetTokenPayload(signInInfo);

        JsonDocument value = JsonSerializer.SerializeToDocument(tokenPayload.Data);
        JsonDocument context = JsonSerializer.SerializeToDocument(tokenPayload.Context);

        if (userLogin.ThirdPartyToken == null)
        {
            LogCreatingThirdPartyToken(userLogin.UserId, ServiceType, signInInfo.ExternalLoginInfo.ProviderKey);

            userLogin.ThirdPartyToken = new ThirdPartyToken
            {
                ServiceType = ServiceType,
                ServiceAccountId = signInInfo.ExternalLoginInfo.ProviderKey,
                TokenType = ThirdPartyToken.DefaultTokenType,
                Value = value,
                Context = context,
            };
        }
        else
        {
            // TODO: validate that we are not reducing the scope

            LogUpdatingThirdPartyToken(userLogin.UserId, ServiceType, signInInfo.ExternalLoginInfo.ProviderKey);

            userLogin.ThirdPartyToken.Value = value;
            userLogin.ThirdPartyToken.Context = context;
        }
    }

    [LoggerMessage(
        LogLevel.Debug,
        "Creating third party token. User: {userId}. Service: {service}. Service ID: {serviceId}"
    )]
    private partial void LogCreatingThirdPartyToken(Guid userId, string service, string serviceId);

    [LoggerMessage(
        LogLevel.Debug,
        "Updating third party token. User: {userId}. Service: {service}. Service ID: {serviceId}"
    )]
    private partial void LogUpdatingThirdPartyToken(Guid userId, string service, string serviceId);

    private OAuthTokenPayload GetTokenPayload(ExternalSignInInfo signInInfo)
    {
        IEnumerable<AuthenticationToken> tokens =
            signInInfo.ExternalLoginInfo.AuthenticationTokens ??
            throw new InvalidOperationException("Missing AuthenticationTokens when signing in with " + LoginProvider);

        Dictionary<string, string> tokensByName = tokens
            // TODO: const (and below)
            .Where(token => token.Name is "access_token" or "refresh_token" or "expires_at")
            .ToDictionary(token => token.Name, token => token.Value);

        if (!tokensByName.TryGetValue("access_token", out string? accessToken))
        {
            throw new InvalidOperationException("Missing access_token when signing in with " + LoginProvider);
        }

        if (!tokensByName.TryGetValue("expires_at", out string? expiresAt))
        {
            throw new InvalidOperationException("Missing expires_at when signing in with " + LoginProvider);
        }

        return new OAuthTokenPayload
        {
            Data = new OAuthTokenData(accessToken, tokensByName.GetValueOrDefault("refresh_token")),
            Context = new OAuthTokenContext
            {
                ExpiresAt = Instant.FromDateTimeOffset(DateTimeOffset.Parse(expiresAt)),
            },
        };
    }
}