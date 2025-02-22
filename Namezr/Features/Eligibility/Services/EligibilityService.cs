using Namezr.Client.Types;

namespace Namezr.Features.Eligibility.Services;

public interface IEligibilityService
{
    IEnumerable<EligibilityDescriptor> GetEligibilityDescriptorsFromAllSupportPlans(
        IEnumerable<SupportPlan> supportPlans
    );
}

[RegisterSingleton]
public class EligibilityService : IEligibilityService
{
    public IEnumerable<EligibilityDescriptor> GetEligibilityDescriptorsFromAllSupportPlans(
        IEnumerable<SupportPlan> supportPlans
    )
    {
        return DoGetEligibilityDescriptorsFromAllSupportPlans(supportPlans)
            .Select(data => new EligibilityDescriptor(data));
    }

    private static IEnumerable<EligibilityDescriptorData> DoGetEligibilityDescriptorsFromAllSupportPlans(
        IEnumerable<SupportPlan> supportPlans
    )
    {
        foreach (SupportPlan supportPlan in supportPlans)
        {
            yield return new EligibilityDescriptorData(supportPlan);
        }

        yield return new EligibilityDescriptorData(VirtualEligibilityType.NoSupportPlanAtAll);
        yield return new EligibilityDescriptorData(VirtualEligibilityType.NoSupportPlanFromListed);
        yield return new EligibilityDescriptorData(VirtualEligibilityType.NotListedSupportPlan);
    }
}