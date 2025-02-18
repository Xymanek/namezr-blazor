using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Creators.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Creators.Services;

public interface ISupportPlansService
{
    Task<IEnumerable<SupportPlan>> GetSupportPlans(Guid creatorId);
}

[AutoConstructor]
[RegisterSingleton]
public partial class SupportPlansService : ISupportPlansService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;

    public async Task<IEnumerable<SupportPlan>> GetSupportPlans(Guid creatorId)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        SupportTargetEntity[] supportTargets = await dbContext.SupportTargets
            .Where(x => x.CreatorId == creatorId)
            .Include(x => x.SupportPlansInfos)
            .ToArrayAsync();

        // TODO: how to deal with "virtual" support targets, i.e. "none", "other (not in list)"?
        return supportTargets.SelectMany(GetSupportPlans);
    }

    private static IEnumerable<SupportPlan> GetSupportPlans(SupportTargetEntity supportTarget)
    {
        switch (supportTarget.ServiceType)
        {
            case SupportServiceType.Twitch:
                yield return new SupportPlan
                {
                    ServiceType = SupportServiceType.Twitch,
                    SupportTargetId = supportTarget.Id,
                    SupportPlanId = TwitchSupportTargetIds.Tier1,
                    DisplayName = "Tier 1",
                };
                yield return new SupportPlan
                {
                    ServiceType = SupportServiceType.Twitch,
                    SupportTargetId = supportTarget.Id,
                    SupportPlanId = TwitchSupportTargetIds.Tier2,
                    DisplayName = "Tier 2",
                };
                yield return new SupportPlan
                {
                    ServiceType = SupportServiceType.Twitch,
                    SupportTargetId = supportTarget.Id,
                    SupportPlanId = TwitchSupportTargetIds.Tier3,
                    DisplayName = "Tier 3",
                };
                yield return new SupportPlan
                {
                    ServiceType = SupportServiceType.Twitch,
                    SupportTargetId = supportTarget.Id,
                    SupportPlanId = TwitchSupportTargetIds.Follower,
                    DisplayName = "Follower",
                };

                break;

            case SupportServiceType.Patreon:
            {
                foreach (SupportPlanInfoEntity supportPlansInfo in supportTarget.SupportPlansInfos!)
                {
                    yield return new SupportPlan
                    {
                        ServiceType = SupportServiceType.Patreon,
                        SupportTargetId = supportTarget.Id,
                        SupportPlanId = supportPlansInfo.SupportPlanId,
                        DisplayName =
                            supportPlansInfo.DisplayName ??
                            throw new Exception("Patreon tier support plan has no display name"),
                    };
                }

                break;
            }
        }
    }
}