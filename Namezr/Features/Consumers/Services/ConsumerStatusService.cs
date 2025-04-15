using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Data;
using Namezr.Features.Creators.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Consumers.Services;

public interface IConsumerStatusService
{
    Task<IReadOnlySet<SupportPlanFullId>> GetUserActiveSupportPlans(
        Guid userId, Guid creatorId,
        UserStatusSyncEagerness eagerness
    );
}

[AutoConstructor]
[RegisterSingleton]
public partial class ConsumerStatusService : IConsumerStatusService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IEnumerable<IConsumerStatusManager> _managers;
    private readonly TargetConsumerService _consumerService;

    // TODO: is this the correct return? Maybe the API consumer will want to group somehow else, e.g. by (support target, plan ID)?
    // TODO: some kind of wrapper utility that fills in missing support plans (as inactive)
    // TODO: lots of info duplication (creator, consumer), maybe some kind of wrapper that will hold that info? 
    // public async Task<ListDictionary<Guid, UserSupportStatusEntry>> GetUserSupportStatuses(
    //     Guid userId, Guid creatorId,
    //     UserStatusSyncEagerness eagerness
    // )
    // {
    //     Dictionary<SupportServiceType, IConsumerStatusManager> managersPerService
    //         = _managers.ToDictionary(x => x.ServiceType);
    //
    //     await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();
    //
    //     SupportTargetEntity[] supportTargets = await dbContext.SupportTargets
    //         .Where(x => x.CreatorId == creatorId)
    //         .ToArrayAsync();
    //
    //     ListDictionary<Guid, UserSupportStatusEntry> result = new();
    //
    //     foreach (SupportTargetEntity supportTarget in supportTargets)
    //     {
    //         //
    //     }
    //
    //     return result;
    // }

    public async Task<IReadOnlySet<SupportPlanFullId>> GetUserActiveSupportPlans(
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

        HashSet<SupportPlanFullId> result = new();

        foreach (SupportTargetEntity supportTarget in supportTargets)
        {
            // TODO: NoSyncSkipConsumerIfMissing
            ICollection<TargetConsumerEntity> consumers =
                await _consumerService.GetOrCreateConsumers(userId, supportTarget.Id);

            foreach (TargetConsumerEntity consumer in consumers)
            {
                ICollection<UserSupportStatusEntry> status = await managersPerService[supportTarget.ServiceType]
                    .GetUserSupportStatuses(consumer.Id, eagerness);

                result.UnionWith(status.Where(x => x.Data.IsActive).Select(x => x.SupportPlanFullId));
            }
        }

        return result;
    }
}