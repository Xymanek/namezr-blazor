using System.Text.Json;
using AspNet.Security.OAuth.Discord;
using CommunityToolkit.Diagnostics;
using Discord;
using Discord.Rest;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Namezr.Features.Identity.Data;
using Namezr.Features.ThirdParty;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.OAuth;

namespace Namezr.Infrastructure.Discord;

public interface IDiscordApiProvider
{
    [MustDisposeResource]
    Task<DiscordRestClient> GetDiscordApiForApp();

    [MustDisposeResource]
    Task<DiscordRestClient> GetDiscordApiForUser(Guid userId);

    [MustDisposeResource]
    Task<DiscordRestClient> GetDiscordApi(ThirdPartyToken token);
}

[AutoConstructor]
[RegisterSingleton]
public partial class DiscordApiProvider : IDiscordApiProvider
{
    private readonly IOptionsMonitor<DiscordAppOptions> _discordOptions;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public async Task<DiscordRestClient> GetDiscordApiForApp()
    {
        DiscordAppOptions options = _discordOptions.CurrentValue;
        
        // TODO: this is a giant hack that also happens to destroy the caching mechanism.
        // Need to convert to a proper websockets-based client.
        
        DiscordRestClient client = new();
        await client.LoginAsync(TokenType.Bot, options.BotToken);

        return client;
    }
    
    [MustDisposeResource]
    public async Task<DiscordRestClient> GetDiscordApiForUser(Guid userId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ApplicationUserLogin userLogin = await dbContext.UserLogins
            .Include(x => x.ThirdPartyToken)
            .Where(x => x.UserId == userId && x.LoginProvider == DiscordAuthenticationDefaults.AuthenticationScheme)
            .SingleAsync();

        return await DoGetDiscordApi(userLogin.ThirdPartyToken!);
    }

    [MustDisposeResource]
    public async Task<DiscordRestClient> GetDiscordApi(ThirdPartyToken token)
    {
        Guard.IsTrue(token.ServiceType == DiscordConstants.ServiceType);

        return await DoGetDiscordApi(token);
    }

    private static async Task<DiscordRestClient> DoGetDiscordApi(ThirdPartyToken token)
    {
        OAuthTokenData tokenData =
            token.Value.Deserialize<OAuthTokenData>()
            ?? throw new Exception("Deserialized token data is null??");

        // TODO: check if the token has not expired yet

        DiscordRestClient client = new();
        await client.LoginAsync(TokenType.Bearer, tokenData.AccessToken);

        return client;
    }
}