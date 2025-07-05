using FluentValidation;
using Namezr.Client.Types;

namespace Namezr.Client.Studio.Eligibility.Edit;

public class EligibilityOptionEditModel
{
    public EligibilityPlanId? PlanId { get; set; }

    public string PriorityGroup { get; set; } = string.Empty;
    public decimal PriorityModifier { get; set; } = 1; // TODO: rename: Weight

    /// <summary>
    /// Maximum submissions per user for this eligibility option (1-10).
    /// </summary>
    public int MaxSubmissionsPerUser { get; set; } = 1;

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
                .InclusiveBetween(1, 10)
                .WithMessage("Max submissions per user must be between 1 and 10.");
        }
    }

    public const int PriorityGroupMaxLength = 50;
}