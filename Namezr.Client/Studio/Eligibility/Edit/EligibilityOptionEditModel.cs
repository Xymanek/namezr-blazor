using FluentValidation;
using Namezr.Client.Types;

namespace Namezr.Client.Studio.Eligibility.Edit;

public class EligibilityOptionEditModel
{
    public EligibilityPlanId? PlanId { get; set; }

    public string PriorityGroup { get; set; } = string.Empty;
    public decimal PriorityModifier { get; set; } = 1; // TODO: rename: Weight
    public int? MaxSubmissionsPerUser { get; set; }

    [RegisterSingleton(typeof(IValidator<EligibilityOptionEditModel>))]
    internal sealed class Validator : AbstractValidator<EligibilityOptionEditModel>
    {
        public Validator()
        {
            RuleFor(x => x.PlanId)
                .NotNull();

            RuleFor(x => x.PriorityGroup)
                .MaximumLength(PriorityGroupMaxLength);

            RuleFor(x => x.PriorityModifier)
                .GreaterThan(0);

            RuleFor(x => x.MaxSubmissionsPerUser)
                .GreaterThanOrEqualTo(0)
                .When(x => x.MaxSubmissionsPerUser.HasValue);
        }
    }

    public const int PriorityGroupMaxLength = 50;
}