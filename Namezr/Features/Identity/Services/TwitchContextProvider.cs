using AspNet.Security.OAuth.Twitch;
using Namezr.Infrastructure.Twitch;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Interfaces;

namespace Namezr.Features.Identity.Services;

[AutoConstructor]
[RegisterSingleton(typeof(ILoginContextProvider))]
internal partial class TwitchContextProvider : CachingLoginContextProviderBase
{
    private readonly ITwitchApiProvider _twitchApiProvider;

    public override string Provider => TwitchAuthenticationDefaults.AuthenticationScheme;

    protected override async Task<LoginContext> FetchLoginContextAsync(string providerKey, CancellationToken ct)
    {
        ITwitchAPI twitchApi = await _twitchApiProvider.GetTwitchApi(null! /* TODO */);

        GetUsersResponse response = await twitchApi.Helix.Users.GetUsersAsync(ids: [providerKey]);
        User user = response.Users.Single();

        return new LoginContext
        {
            DisplayName = user.DisplayName,
            ImageUrl = user.ProfileImageUrl,
        };
    }
}