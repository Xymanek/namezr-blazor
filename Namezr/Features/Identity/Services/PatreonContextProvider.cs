using AspNet.Security.OAuth.Patreon;
using CommunityToolkit.Diagnostics;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Patreon;
using Patreon.Net;
using Patreon.Net.Models;

namespace Namezr.Features.Identity.Services;

[AutoConstructor]
[RegisterSingleton(typeof(ILoginContextProvider))]
internal partial class PatreonContextProvider : CachingLoginContextProviderBase
{
    private readonly IPatreonApiProvider _patreonApiProvider;

    public override string Provider => PatreonAuthenticationDefaults.AuthenticationScheme;

    protected override async Task<LoginContext> FetchLoginContextAsync(
        ApplicationUserLogin userLogin,
        CancellationToken ct
    )
    {
        Guard.IsNotNull(userLogin.ThirdPartyToken);

        PatreonClient patreonApi = _patreonApiProvider.GetPatreonApi(userLogin.ThirdPartyToken);
        User user = await patreonApi.GetIdentityAsync();

        return new LoginContext
        {
            DisplayName = user.FullName,
            ImageUrl = user.ImageUrl,
        };
    }
}