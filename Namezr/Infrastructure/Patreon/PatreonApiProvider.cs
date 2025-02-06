using System.Text.Json;
using AspNet.Security.OAuth.Patreon;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Namezr.Features.Identity.Data;
using Namezr.Features.ThirdParty;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.OAuth;
using NodaTime;
using Patreon.Net;
using Patreon.Net.Models;

namespace Namezr.Infrastructure.Patreon;

public interface IPatreonApiProvider
{
    [MustDisposeResource]
    Task<PatreonClient> GetPatreonApiForUser(Guid userId);
}

[AutoConstructor]
[RegisterSingleton]
public partial class PatreonApiProvider : IPatreonApiProvider
{
    private readonly IOptionsMonitor<PatreonAuthenticationOptions> _patreonOptions;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly ILogger<PatreonApiProvider> _logger;
    private readonly IClock _clock;

    [MustDisposeResource]
    public async Task<PatreonClient> GetPatreonApiForUser(Guid userId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .Include(x => x.ThirdPartyToken)
            .Where(x => x.UserId == userId && x.LoginProvider == PatreonAuthenticationDefaults.AuthenticationScheme)
            .SingleAsync();

        return GetPatreonApi(userLogin.ThirdPartyToken!);
    }

    private PatreonClient GetPatreonApi(ThirdPartyToken token)
    {
        OAuthTokenData tokenData =
            token.Value.Deserialize<OAuthTokenData>()
            ?? throw new Exception("Deserialized token data is null??");

        OAuthTokenContext tokenContext =
            token.Context?.Deserialize<OAuthTokenContext>()
            ?? throw new Exception("Deserialized token context is null??");

        PatreonAuthenticationOptions options = _patreonOptions.Get(PatreonAuthenticationDefaults.AuthenticationScheme);

        PatreonClient patreonApi = new(
            tokenData.AccessToken,
            tokenData.RefreshToken,
            options.ClientId,
            tokenContext.ExpiresAt.ToDateTimeOffset()
        );

        patreonApi.TokensRefreshedAsync += newToken => StoreRefreshedToken(token, newToken);

        return patreonApi;
    }

    private async Task StoreRefreshedToken(ThirdPartyToken oldEntity, OAuthToken response)
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
        "Patreon User ID: {patreonUserId}."
    )]
    private partial void LogStoredRefreshedToken(long tokenId, string patreonUserId);
}