﻿using Microsoft.AspNetCore.Authentication.Google;

namespace Namezr.Features.Identity.Services;

[AutoConstructor]
[RegisterSingleton(typeof(ILoginContextProvider))]
internal partial class GoogleContextProvider : CachingLoginContextProviderBase
{
    public override string Provider => GoogleDefaults.AuthenticationScheme;

    protected override Task<LoginContext> FetchLoginContextAsync(string providerKey, CancellationToken ct)
    {
        // TODO: need to store the user's access token
        throw new NotImplementedException();
    }
}