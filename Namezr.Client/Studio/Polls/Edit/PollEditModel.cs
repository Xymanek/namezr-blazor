using FluentValidation;
using Namezr.Client.Studio.Eligibility.Edit;

namespace Namezr.Client.Studio.Polls.Edit;

public class PollEditModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public bool IsAnonymous { get; set; }
    public bool IsOpen { get; set; }
    public bool AllowUsersToAddOptions { get; set; }

    public List<EligibilityOptionEditModel> EligibilityOptions { get; set; } = new();

    public List<PollOptionEditModel> Options { get; set; } = new();

    public void AddBlankOption()
    {
        Options.Add(new PollOptionEditModel
        {
            Id = Guid.CreateVersion7(),
        });
    }

    [RegisterSingleton(typeof(IValidator<PollEditModel>))]
    internal sealed class Validator : AbstractValidator<PollEditModel>
    {
        public Validator(
            IValidator<PollOptionEditModel> fieldValidator,
            IValidator<EligibilityOptionEditModel> eligibilityOptionValidator
        )
        {
            RuleFor(x => x.Title)
                .MinimumLength(3)
                .MaximumLength(TitleMaxLength);

            RuleFor(x => x.Description)
                .MaximumLength(DescriptionMaxLength);

            RuleFor(x => x.Options)
                .NotEmpty();

            RuleFor(x => x.Options)
                .Must(options => options.Select(x => x.Title.Trim()).Distinct().Count() == options.Count)
                .WithMessage("Duplicate options are not allowed");

            RuleForEach(x => x.Options)
                .SetValidator(fieldValidator);

            // TODO: unify with QuestionnaireEditModel
            RuleFor(x => x.EligibilityOptions)
                .Must(options => options.Select(x => x.PlanId).Distinct().Count() == options.Count)
                .WithMessage("Cannot select the same support plan multiple times");

            RuleForEach(x => x.EligibilityOptions)
                .SetValidator(eligibilityOptionValidator);
        }
    }

    public const int TitleMaxLength = 50;
    public const int DescriptionMaxLength = 500;
}

public class PollOptionEditModel
{
    public required Guid Id { get; init; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [RegisterSingleton(typeof(IValidator<PollOptionEditModel>))]
    internal sealed class Validator : AbstractValidator<PollOptionEditModel>
    {
        public Validator()
        {
            RuleFor(x => x.Title)
                .MinimumLength(3)
                .MaximumLength(TitleMaxLength);

            RuleFor(x => x.Description)
                .MaximumLength(DescriptionMaxLength);
        }
    }

    public const int TitleMaxLength = 30;
    public const int DescriptionMaxLength = 500;
}