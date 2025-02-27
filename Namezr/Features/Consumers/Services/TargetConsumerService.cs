using AspNet.Security.OAuth.Twitch;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Consumers.Services;

// TODO: interface
[AutoConstructor]
[RegisterSingleton]
public partial class TargetConsumerService
{
    // TODO: IEnumerable of all interfaces
    private readonly TwitchConsumerService _twitchConsumerService;

    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public async Task<ICollection<TargetConsumerEntity>> GetOrCreateConsumers(Guid userId, Guid supportTargetId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ApplicationUserLogin[] userLogins = await dbContext.UserLogins
            .Where(x => x.UserId == userId)
            .ToArrayAsync();

        SupportTargetEntity supportTarget = await dbContext.SupportTargets
            .SingleAsync(x => x.Id == supportTargetId);

        List<TargetConsumerEntity> result = new(userLogins.Length);

        foreach (ApplicationUserLogin userLogin in userLogins)
        {
            if (
                userLogin.LoginProvider == TwitchAuthenticationDefaults.AuthenticationScheme
                && supportTarget.ServiceType == SupportServiceType.Twitch
            )
            {
                // TODO: parallelize
                result.Add(await _twitchConsumerService.GetOrCreateTwitchConsumer(userLogin, supportTargetId));
            }
        }

        return result;
    }

    public async Task<ICollection<TargetConsumerEntity>> GetOrCreateAssociatedConsumers(Guid userId, Guid creatorId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        ApplicationUserLogin[] userLogins = await dbContext.UserLogins
            .Where(x => x.UserId == userId)
            .ToArrayAsync();

        List<TargetConsumerEntity> result = new(userLogins.Length);

        foreach (ApplicationUserLogin userLogin in userLogins)
        {
            if (userLogin.LoginProvider == TwitchAuthenticationDefaults.AuthenticationScheme)
            {
                // TODO: parallelize
                result.Add(await _twitchConsumerService.GetOrCreateTwitchConsumer(userLogin, creatorId));
            }
        }

        return result;
    }
}