using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Patreon;
using AspNet.Security.OAuth.Twitch;
using LinqKit;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Consumers.Services;

public static class ConsumerUserRetriever
{
    public static IQueryable<ConsumerUser> BuildConsumerUsersQuery(ApplicationDbContext dbContext)
    {
        IQueryable<LoginConsumerEquivalent> equivalents = BuildConsumerEquivalentQuery(dbContext.UserLogins);

        return dbContext.TargetConsumers
            .Join(
                equivalents,
                consumer => new { consumer.ServiceUserId, consumer.SupportTarget.ServiceType },
                equivalent => new { equivalent.ServiceUserId, equivalent.ServiceType },
                (consumer, equivalent) => new ConsumerUser
                {
                    Consumer = consumer,
                    UserLogin = equivalent.UserLogin,
                }
            );
    }

    private static IQueryable<LoginConsumerEquivalent> BuildConsumerEquivalentQuery(
        IQueryable<ApplicationUserLogin> loginQuery
    )
    {
        return loginQuery
            .Select(login => new LoginConsumerEquivalent
            {
                ServiceType = login.LoginProvider == TwitchAuthenticationDefaults.AuthenticationScheme
                    ? SupportServiceType.Twitch
                    : login.LoginProvider == PatreonAuthenticationDefaults.AuthenticationScheme
                        ? SupportServiceType.Patreon
                        : login.LoginProvider == DiscordAuthenticationDefaults.AuthenticationScheme
                            ? SupportServiceType.Discord
                            : (SupportServiceType)(-1),

                ServiceUserId = login.ProviderKey,

                UserLogin = login,
            });
    }

    private class LoginConsumerEquivalent
    {
        public required SupportServiceType ServiceType { get; init; }
        public required string ServiceUserId { get; init; }

        public required ApplicationUserLogin UserLogin { get; init; }
    }

    /// <summary>
    /// Used to include users who are currently participating in your activities (e.g. questionnaires)
    /// but do not have an active link. This can happen if a user has removed their link
    /// or the creator configured a non-support-plan eligibility option, e.g.
    /// <see cref="F:Namezr.Client.Types.VirtualEligibilityType.NoSupportPlanAtAll"/>.
    /// </summary>
    public static IQueryable<ApplicationUser> GetParticipatingUsers(
        Guid creatorId,
        IQueryable<ApplicationUser> users
    )
    {
        ExpressionStarter<ApplicationUser> filter = PredicateBuilder.New<ApplicationUser>();

        filter = filter.Or(user => user.Submissions!.Any(submission =>
            submission.Version.Questionnaire.CreatorId == creatorId
        ));

        return users.Where(filter);
    }
}

public class ConsumerUser
{
    public required TargetConsumerEntity Consumer { get; init; }
    public required ApplicationUserLogin UserLogin { get; init; }
}