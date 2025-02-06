using AspNet.Security.OAuth.Twitch;
using Namezr.Infrastructure.Auth;
using Namezr.Infrastructure.OAuth;

namespace Namezr.Features.Twitch;

[AutoConstructor]
[RegisterSingleton(typeof(ILoginProviderHandler))]
internal partial class TwitchLoginHandler : OAuthLoginHandler
{
    /// <inheritdoc />
    public override string LoginProvider => TwitchAuthenticationDefaults.AuthenticationScheme;

    /// <inheritdoc />
    protected override string ServiceType => TwitchConstants.ServiceType;
}