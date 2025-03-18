using AspNet.Security.OAuth.Discord;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Namezr.Infrastructure.Discord;

[AutoConstructor]
public partial class NamezrDiscordAuthenticationHandler : DiscordAuthenticationHandler
{
    protected override string BuildChallengeUrl(
        AuthenticationProperties properties,
        string redirectUri
    )
    {
        string challengeUrl = base.BuildChallengeUrl(properties, redirectUri);

        Dictionary<string, string?> newParams = [];

        ICollection<string>? additionalScopes = properties.GetParameter<ICollection<string>>(
            DiscordChallengeProperties.AdditionalScopes
        );
        if (additionalScopes != null)
        {
            string newScopes = string.Join(' ', additionalScopes);

            Dictionary<string, StringValues> existingQuery = QueryHelpers.ParseQuery(challengeUrl);

            string existingScopes = "";
            if (existingQuery.TryGetValue("scope", out StringValues existingScopeValues))
            {
                existingScopes = string.Join(' ', (IEnumerable<string>)existingScopeValues);
            }

            // TODO: this does not actually replace the existing scope param
            // (but currently still works)
            newParams.Add("scope", (existingScopes + " " + newScopes).Trim());
        }

        ulong? guildId = properties.GetParameter<ulong?>(DiscordChallengeProperties.GuildId);
        if (guildId.HasValue)
        {
            newParams.Add("guild_id", guildId.Value.ToString());
        }

        bool? disableGuildSelect = properties.GetParameter<bool?>(DiscordChallengeProperties.DisableGuildSelect);
        if (disableGuildSelect.HasValue)
        {
            newParams.Add("disable_guild_select", disableGuildSelect.Value.ToString().ToLower());
        }

        if (newParams.Any())
        {
            challengeUrl = QueryHelpers.AddQueryString(challengeUrl, newParams);
        }

        return challengeUrl;
    }
}