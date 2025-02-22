namespace Namezr.Client.Types;

public enum VirtualEligibilityType
{
    /// <summary>
    /// User is currently not active on any support plan
    /// </summary>
    NoSupportPlanAtAll = 1,

    /// <summary>
    /// User is currently not active on any of the listed support plans
    /// </summary>
    NoSupportPlanFromListed = 2,

    /// <summary>
    /// User is currently active on a support plan that is not listed
    /// </summary>
    NotListedSupportPlan = 3,
}