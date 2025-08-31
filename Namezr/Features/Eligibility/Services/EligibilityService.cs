using System.Collections.Immutable;
using System.Diagnostics;
using AutoRegisterInject;
using CommunityToolkit.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Types;
using Namezr.Features.Consumers.Services;
using Namezr.Features.Eligibility.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Eligibility.Services;

public interface IEligibilityService
{
    IEnumerable<EligibilityPlan> GetEligibilityDescriptorsFromAllSupportPlans(
        IEnumerable<SupportPlan> supportPlans
    );

    /// <param name="userId"></param>
    /// <param name="configuration">
    ///     <see cref="EligibilityConfigurationEntity.Options"/> must be loaded and non-empty.
    /// </param>
    /// <param name="syncEagerness"></param>
    Task<EligibilityResult> ClassifyEligibility(
        Guid userId, EligibilityConfigurationEntity configuration,
        UserStatusSyncEagerness syncEagerness
    );

    /// <summary>
    /// Fetches the cached eligibility. Will not query the DB or external services
    /// </summary>
    /// <returns>Null if the requested pair was not found in cache</returns>
    Task<EligibilityResult?> GetCachedEligibilityAsync(Guid userId, EligibilityConfigurationEntity configuration);
}

public static class EligibilityServiceExtensions
{
    public static async Task<EligibilityResult> GetCachedEligibilityOrClassify(
        this IEligibilityService service,
        Guid userId, EligibilityConfigurationEntity configuration,
        UserStatusSyncEagerness syncEagerness
    )
    {
        EligibilityResult? result = await service.GetCachedEligibilityAsync(userId, configuration);
        
        if (result != null)
        {
            return result;
        }

        return await service.ClassifyEligibility(
            userId, configuration, syncEagerness
        );
    }
}

[AutoConstructor]
[RegisterSingleton]
internal partial class EligibilityService : IEligibilityService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IConsumerStatusService _statusService;
    private readonly IEligibilityCache _cache;

    public IEnumerable<EligibilityPlan> GetEligibilityDescriptorsFromAllSupportPlans(
        IEnumerable<SupportPlan> supportPlans
    )
    {
        foreach (SupportPlan supportPlan in supportPlans)
        {
            yield return new EligibilityPlan(supportPlan)
            {
                DefaultPriorityGroup = supportPlan.ServiceType.ToString(),
            };
        }

        yield return new EligibilityPlan(VirtualEligibilityType.NoSupportPlanAtAll);
        yield return new EligibilityPlan(VirtualEligibilityType.NoSupportPlanFromListed);
        yield return new EligibilityPlan(VirtualEligibilityType.NotListedSupportPlan);
    }

    // TODO: evaluate usages of this to call the mass sync where applicable
    /// <param name="userId"></param>
    /// <param name="configuration">
    ///     <see cref="EligibilityConfigurationEntity.Options"/> must be loaded and non-empty.
    /// </param>
    /// <param name="syncEagerness"></param>
    public async Task<EligibilityResult> ClassifyEligibility(
        Guid userId, EligibilityConfigurationEntity configuration,
        UserStatusSyncEagerness syncEagerness
    )
    {
        using Activity? activity = Diagnostics.ActivitySource.StartActivity();
        activity?.SetTag("UserId", userId);
        activity?.SetTag("EligibilityConfigurationId", configuration.Id);

        Guard.IsTrue(configuration.Options?.Count > 0);

        HashSet<SupportPlanFullId> relevantSupportPlanIds = configuration.Options
            .Where(option => option.PlanId.Type == EligibilityType.SupportPlan)
            .Select(option => option.PlanId.SupportPlanId!)
            .Distinct()
            .ToHashSet();

        // https://github.com/dotnet/efcore/pull/35719
        IEnumerable<Guid> relevantSupportTargetIds = relevantSupportPlanIds
            .Select(planFullId => planFullId.SupportTargetId)
            .Distinct()
            .ToArray();

        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync();

        Guid creatorId = await dbContext.Creators
            .Where(c => c.SupportTargets!.Any(t => relevantSupportTargetIds.Contains(t.Id)))
            .Select(c => c.Id)
            .Distinct()
            .SingleAsync();

        IReadOnlySet<SupportPlanFullId> userActiveSupportPlans = await _statusService
            // TODO: we can be more granular here than "all creator", i.e. skip non-relevant support targets
            // though need to consider also VirtualEligibilityType.NoSupportPlanAtAll
            .GetUserActiveSupportPlans(userId, creatorId, syncEagerness);

        Dictionary<EligibilityPlanId, bool> isMatchingPerEligibilityPlan = configuration.Options
            .ToDictionary(option => option.PlanId, option =>
            {
                return option.PlanId.Type switch
                {
                    EligibilityType.SupportPlan => userActiveSupportPlans.Contains(option.PlanId.SupportPlanId!),

                    EligibilityType.Virtual => option.PlanId.VirtualEligibilityType switch
                    {
                        VirtualEligibilityType.NoSupportPlanAtAll => userActiveSupportPlans.Count == 0,

                        VirtualEligibilityType.NoSupportPlanFromListed => relevantSupportPlanIds
                            .Union(userActiveSupportPlans)
                            .None(),

                        VirtualEligibilityType.NotListedSupportPlan => userActiveSupportPlans
                            .Except(relevantSupportPlanIds)
                            .Any(),

                        _ => throw new UnreachableException(),
                    },

                    _ => throw new UnreachableException(),
                };
            });

        if (isMatchingPerEligibilityPlan.Values.All(x => !x))
        {
            return new EligibilityResult
            {
                EligiblePlanIds = ImmutableHashSet<EligibilityPlanId>.Empty,
            };
        }

        IGrouping<string, EligibilityOptionEntity>[] optionsByPriorityGroup = configuration.Options
            .GroupBy(option => option.PriorityGroup)
            .ToArray();

        // TODO: account for empty string being an individual group 
        decimal modifier = optionsByPriorityGroup
            .Select(group => group
                .Where(option => isMatchingPerEligibilityPlan[option.PlanId])
                .Select(option => option.PriorityModifier)
                .Prepend(0) // Max fails if empty sequence and "0" effectively means uneligible
                .Max()
            )
            .Sum();

        // Calculate max submissions per user from eligible options
        int maxSubmissionsPerUser = configuration.Options
            .Where(option => isMatchingPerEligibilityPlan[option.PlanId])
            .Select(option => option.MaxSubmissionsPerUser)
            .DefaultIfEmpty(1)
            .Max();

        EligibilityResult result = new()
        {
            EligiblePlanIds = isMatchingPerEligibilityPlan
                .Where(x => x.Value)
                .Select(x => x.Key)
                .ToImmutableHashSet(),

            Modifier = modifier,
            MaxSubmissionsPerUser = maxSubmissionsPerUser,
        };

        // TODO: currently we care only about questionnaire submissions. Limit this logic somehow? 
        await _cache.SetAsync(userId, configuration, result, CachedExpirationTime);

        return result;
    }

    // Think about this - no eligibility should be cached for shorter?
    private static readonly TimeSpan CachedExpirationTime = TimeSpan.FromHours(5);

    public async Task<EligibilityResult?> GetCachedEligibilityAsync(Guid userId, EligibilityConfigurationEntity configuration)
    {
        return await _cache.GetAsync(userId, configuration);
    }
}

file static class EnumerableExtensions
{
    public static bool None<T>(this IEnumerable<T> source)
        => !source.Any();
}