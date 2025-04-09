﻿using AspNet.Security.OAuth.Discord;
using Discord.Rest;
using Namezr.Infrastructure.Discord;

namespace Namezr.Features.Identity.Services;

[AutoConstructor]
[RegisterSingleton(typeof(ILoginContextProvider))]
internal partial class DiscordContextProvider
    // TODO: Do not use CachingLoginContextProviderBase once we implement
    // full discord client since the client will be caching internally
    : CachingLoginContextProviderBase
{
    private readonly IDiscordApiProvider _apiProvider;

    public override string Provider => DiscordAuthenticationDefaults.AuthenticationScheme;

    protected override async Task<LoginContext> FetchLoginContextAsync(string providerKey, CancellationToken ct)
    {
        await using DiscordRestClient client = await _apiProvider.GetDiscordApiForApp();

        RestUser user = await client.GetUserAsync(ulong.Parse(providerKey));

        return new LoginContext
        {
            DisplayName = user.Username,
            ImageUrl = user.GetAvatarUrl(),
        };
    }
}