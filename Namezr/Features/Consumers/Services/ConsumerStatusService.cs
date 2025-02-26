using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Infrastructure.Data;
using Singulink.Collections;

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

    // TODO: is this the correct return? Maybe the API consumer will want to group somehow else, e.g. by (support target, plan ID)?
    // TODO: some kind of wrapper utility that fills in missing support plans (as inactive)
    // TODO: lots of info duplication (creator, consumer), maybe some kind of wrapper that will hold that info? 
    public async Task<ListDictionary<Guid, UserSupportStatusEntry>> GetUserSupportStatuses(
        Guid userId, Guid creatorId,
        UserStatusSyncEagerness eagerness
    )
    {
        Dictionary<SupportServiceType, IConsumerStatusManager> managersPerService
            = _managers.ToDictionary(x => x.ServiceType);

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SupportTargetEntity[] supportTargets = await dbContext.SupportTargets
            .Where(x => x.CreatorId == creatorId)
            .ToArrayAsync();
        
        ListDictionary<Guid, UserSupportStatusEntry> result = new();
        
        foreach (SupportTargetEntity supportTarget in supportTargets)
        {
            //
        }
        
        return result;
    }
}