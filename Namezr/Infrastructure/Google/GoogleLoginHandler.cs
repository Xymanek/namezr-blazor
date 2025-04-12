using Microsoft.AspNetCore.Authentication.Google;
using Namezr.Infrastructure.Auth;
using Namezr.Infrastructure.OAuth;

namespace Namezr.Infrastructure.Google;

[AutoConstructor]
[RegisterSingleton(typeof(ILoginProviderHandler))]
internal partial class GoogleLoginHandler : OAuthLoginHandler
{
    /// <inheritdoc />
    public override string LoginProvider => GoogleDefaults.AuthenticationScheme;

    /// <inheritdoc />
    protected override string ServiceType => GoogleConstants.ServiceType;
}