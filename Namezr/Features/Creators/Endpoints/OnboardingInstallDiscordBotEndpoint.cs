using AspNet.Security.OAuth.Discord;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Namezr.Components.Account;
using Namezr.Features.Creators.Services;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Discord;

namespace Namezr.Features.Creators.Endpoints;

[Handler]
[Behaviors] // Remove the default validator
[MapPost(Route)]
internal partial class OnboardingInstallDiscordBotEndpoint
{
    public const string Route = "/studio/onboarding/discord/install-bot";

    public class Command
    {
        public required ulong GuildId { get; init; }
    }

    private static async ValueTask<IResult> HandleAsync(
        [FromForm] Command command,
        IHttpContextAccessor httpContextAccessor,
        IdentityUserAccessor userAccessor,
        ICreatorOnboardingService onboardingService
    )
    {
        ApplicationUser user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);
        IReadOnlyList<PotentialSupportTarget> potentialSupportTargets
            = await onboardingService.GetPotentialSupportTargets(user.Id);

        bool legalGuildId = potentialSupportTargets.Any(
            target =>
                target is PotentialDiscordSupportTarget discordTarget &&
                discordTarget.GuildId == command.GuildId
        );

        if (!legalGuildId)
        {
            // TODO: redirect to the onboarding page with a toast
            throw new Exception("Illegal guild ID");
        }

        AuthenticationProperties properties = new()
        {
            Parameters =
            {
                [DiscordChallengeProperties.GuildId] = command.GuildId,
                [DiscordChallengeProperties.DisableGuildSelect] = true,
                [DiscordChallengeProperties.AdditionalScopes] = new[]
                {
                    "bot",
                },

                // TODO: return url
            }
        };

        return Results.Challenge(properties, [DiscordAuthenticationDefaults.AuthenticationScheme]);
    }
}