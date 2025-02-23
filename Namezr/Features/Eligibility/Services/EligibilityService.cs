using Namezr.Client.Types;

namespace Namezr.Features.Eligibility.Services;

public interface IEligibilityService
{
    IEnumerable<EligibilityPlan> GetEligibilityDescriptorsFromAllSupportPlans(
        IEnumerable<SupportPlan> supportPlans
    );
}

[RegisterSingleton]
public class EligibilityService : IEligibilityService
{
    public IEnumerable<EligibilityPlan> GetEligibilityDescriptorsFromAllSupportPlans(
        IEnumerable<SupportPlan> supportPlans
    )
    {
        foreach (SupportPlan supportPlan in supportPlans)
        {
            yield return new EligibilityPlan(supportPlan);
        }

        yield return new EligibilityPlan(VirtualEligibilityType.NoSupportPlanAtAll);
        yield return new EligibilityPlan(VirtualEligibilityType.NoSupportPlanFromListed);
        yield return new EligibilityPlan(VirtualEligibilityType.NotListedSupportPlan);
    }
}