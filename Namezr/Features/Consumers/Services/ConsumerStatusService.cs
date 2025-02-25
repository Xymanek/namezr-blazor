using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Consumers.Services;

public interface IConsumerStatusService
{
    Task SyncOutdatedForAllTargets(Guid userId, Guid creatorId);
}

[AutoConstructor]
[RegisterSingleton]
public partial class ConsumerStatusService : IConsumerStatusService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IEnumerable<IConsumerStatusManager> _managers;

    // TODO: IEnumerable
    private readonly TwitchConsumerService _twitchConsumerService;

    public async Task SyncOutdatedForAllTargets(Guid userId, Guid creatorId)
    {
        Dictionary<SupportServiceType, IConsumerStatusManager> managersPerService
            = _managers.ToDictionary(x => x.ServiceType);

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SupportTargetEntity[] supportTargets = await dbContext.SupportTargets
            .Where(x => x.CreatorId == creatorId)
            .ToArrayAsync();

        // TODO: parallelize
        foreach (SupportTargetEntity supportTarget in supportTargets)
        {
            TargetConsumerEntity consumer = supportTarget.ServiceType switch
            {
                SupportServiceType.Twitch => await _twitchConsumerService.GetOrCreateTwitchConsumer(
                    userId, supportTarget.Id
                ),

                SupportServiceType.Patreon or SupportServiceType.KoFi or SupportServiceType.BuyMeACoffee
                    => throw new NotImplementedException(),

                _ => throw new UnreachableException(),
            };

            await managersPerService[supportTarget.ServiceType].SyncConsumerStatus(consumer.Id);
        }
    }
}