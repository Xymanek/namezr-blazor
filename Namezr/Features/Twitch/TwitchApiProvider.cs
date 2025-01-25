using System.Text.Json;
using AspNet.Security.OAuth.Twitch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;
using TwitchLib.Api;
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
    private readonly ILoggerFactory _loggerFactory;

    public async Task<ITwitchAPI> GetTwitchApiForUser(Guid userId /* TODO: enum to get either personal or creator token */)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .Include(x => x.ThirdPartyToken)
            .Where(x => x.UserId == userId && x.LoginProvider == TwitchAuthenticationDefaults.AuthenticationScheme)
            .SingleAsync();

        TwitchTokenData tokenData =
            userLogin.ThirdPartyToken!.Value.Deserialize<TwitchTokenData>()
            ?? throw new Exception("Deserialized token data is null??");

        TwitchAuthenticationOptions twitchOptions = _twitchOptions
            .Get(TwitchAuthenticationDefaults.AuthenticationScheme);

        return new TwitchAPI(_loggerFactory)
        {
            Settings =
            {
                ClientId = twitchOptions.ClientId,
                AccessToken = tokenData.AccessToken,
            }
        };
    }
}