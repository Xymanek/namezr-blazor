using AspNet.Security.OAuth.Patreon;
using Namezr.Infrastructure.Auth;
using Namezr.Infrastructure.OAuth;

namespace Namezr.Infrastructure.Patreon;

[AutoConstructor]
[RegisterSingleton(typeof(ILoginProviderHandler))]
internal partial class PatreonLoginHandler : OAuthLoginHandler
{
    /// <inheritdoc />
    public override string LoginProvider => PatreonAuthenticationDefaults.AuthenticationScheme;

    /// <inheritdoc />
    protected override string ServiceType => PatreonConstants.ServiceType;
}