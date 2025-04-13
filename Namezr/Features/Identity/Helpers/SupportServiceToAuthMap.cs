using System.Diagnostics;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Patreon;
using AspNet.Security.OAuth.Twitch;
using Namezr.Client.Types;

namespace Namezr.Features.Identity.Helpers;

internal static class SupportServiceToAuthMap
{
    public static bool HasSupportServiceLink(SupportServiceType serviceType, IEnumerable<string> userAuthSchemes)
    {
        string authScheme = ServiceTypeToAuthScheme(serviceType);

        return userAuthSchemes.Contains(authScheme);
    }

    public static string ServiceTypeToAuthScheme(SupportServiceType serviceType)
    {
        return serviceType switch
        {
            SupportServiceType.Twitch => TwitchAuthenticationDefaults.AuthenticationScheme,
            SupportServiceType.Patreon => PatreonAuthenticationDefaults.AuthenticationScheme,
            SupportServiceType.Discord => DiscordAuthenticationDefaults.AuthenticationScheme,

            _ => throw new UnreachableException(),
        };
    }
}