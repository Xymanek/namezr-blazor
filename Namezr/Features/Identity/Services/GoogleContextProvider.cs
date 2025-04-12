using CommunityToolkit.Diagnostics;
using Google.Apis.PeopleService.v1;
using Google.Apis.PeopleService.v1.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Google;

namespace Namezr.Features.Identity.Services;

[AutoConstructor]
[RegisterSingleton(typeof(ILoginContextProvider))]
internal partial class GoogleContextProvider : CachingLoginContextProviderBase
{
    private readonly IGoogleApiProvider _googleApiProvider;

    public override string Provider => GoogleDefaults.AuthenticationScheme;

    protected override async Task<LoginContext> FetchLoginContextAsync(
        ApplicationUserLogin userLogin, CancellationToken ct
    )
    {
        Guard.IsNotNull(userLogin.ThirdPartyToken);

        BaseClientService.Initializer apiInitializer = await _googleApiProvider.GetInitializer(
            userLogin.ThirdPartyToken, ct
        );

        PeopleServiceService peopleService = new(apiInitializer);

        PeopleResource.GetRequest request = peopleService.People.Get("people/me");
        request.PersonFields = "names,photos";

        Person person = await request.ExecuteAsync(ct);
        Name name = person.Names.First();

        return new LoginContext
        {
            DisplayName = name.DisplayName ?? name.UnstructuredName,
            ImageUrl = person.Photos.FirstOrDefault()?.Url,
        };
    }
}