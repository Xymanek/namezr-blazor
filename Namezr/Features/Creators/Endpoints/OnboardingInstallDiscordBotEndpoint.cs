using AspNet.Security.OAuth.Discord;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Creators.Services;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Discord;

namespace Namezr.Features.Creators.Endpoints;

[Handler]
[AutoConstructor]
[Authorize]
[Behaviors] // Remove the default validator
[MapPost(Route)]
internal sealed partial class OnboardingInstallDiscordBotEndpoint
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly ICreatorOnboardingService _onboardingService;

    public const string Route = "/studio/onboarding/discord/install-bot";

    public class Command
    {
        public required ulong GuildId { get; init; }
    }

    private async ValueTask<IResult> HandleAsync(
        [FromForm] Command command
    )
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext!;

        ApplicationUser user = await _userAccessor.GetRequiredUserAsync(httpContext);
        IReadOnlyList<PotentialSupportTarget> potentialSupportTargets
            = await _onboardingService.GetPotentialSupportTargets(user.Id);

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
            RedirectUri =
                // TODO: this is bad for security - validate correct path base
                httpContext.Request.Headers.Referer.FirstOrDefault() ??

                // TODO: ideally handle the error case
                // Until then, we can't pre-select the target
                UriHelper.BuildRelative(
                    httpContext.Request.PathBase,
                    "/studio/onboarding"
                ),

            Parameters =
            {
                [DiscordChallengeProperties.GuildId] = command.GuildId,
                [DiscordChallengeProperties.DisableGuildSelect] = true,
                [DiscordChallengeProperties.AdditionalScopes] = new[]
                {
                    "bot",
                },
            }
        };

        return Results.Challenge(properties, [DiscordAuthenticationDefaults.AuthenticationScheme]);
    }
}