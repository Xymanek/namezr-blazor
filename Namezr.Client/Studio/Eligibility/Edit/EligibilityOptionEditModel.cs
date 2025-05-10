using FluentValidation;
using Namezr.Client.Types;

namespace Namezr.Client.Studio.Eligibility.Edit;

public class EligibilityOptionEditModel
{
    public EligibilityPlanId? PlanId { get; set; }

    public string PriorityGroup { get; set; } = string.Empty;
    public decimal PriorityModifier { get; set; } = 1; // TODO: rename: Weight

    public int? SelectionWave { get; set; }

    [RegisterSingleton(typeof(IValidator<EligibilityOptionEditModel>))]
    internal sealed class Validator : AbstractValidator<EligibilityOptionEditModel>
    {
        public Validator()
        {
            RuleFor(x => x.PlanId)
                .NotNull();

            RuleFor(x => x.PriorityGroup)
                .MaximumLength(PriorityGroupMaxLength);

            RuleFor(x => x.SelectionWave);

            RuleFor(x => x.PriorityModifier)
                .GreaterThan(0);
        }
    }

    public const int PriorityGroupMaxLength = 50;
}