using AspNet.Security.OAuth.Discord;
using Namezr.Infrastructure.Auth;
using Namezr.Infrastructure.OAuth;

namespace Namezr.Infrastructure.Discord;

[AutoConstructor]
[RegisterSingleton(typeof(ILoginProviderHandler))]
internal partial class DiscordLoginHandler : OAuthLoginHandler
{
    /// <inheritdoc />
    public override string LoginProvider => DiscordAuthenticationDefaults.AuthenticationScheme;

    /// <inheritdoc />
    protected override string ServiceType => DiscordConstants.ServiceType;
}