using FluentValidation;

namespace Namezr.Client.Shared;

public class SubmissionAttributeModel
{
    public required string Key { get; set; }
    public required string Value { get; set; }

    public const int KeyMaxLength = 50;

    [RegisterSingleton(typeof(IValidator<SubmissionAttributeModel>))]
    public class SubmissionAttributeModelValidator : AbstractValidator<SubmissionAttributeModel>
    {
        public SubmissionAttributeModelValidator()
        {
            RuleFor(model => model.Key)
                .NotEmpty()
                .MaximumLength(KeyMaxLength);

            RuleFor(model => model.Value)
                .NotNull();
        }
    }
}