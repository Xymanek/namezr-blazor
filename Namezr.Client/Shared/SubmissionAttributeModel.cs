using FluentValidation;

namespace Namezr.Client.Shared;

public class SubmissionAttributeModel
{
    public string? Key { get; set; }
    public string? Value { get; set; }

    public const int KeyMaxLength = 50;
    public const int ValueMaxLength = 5000;

    [RegisterSingleton(typeof(IValidator<SubmissionAttributeModel>))]
    public class SubmissionAttributeModelValidator : AbstractValidator<SubmissionAttributeModel>
    {
        public SubmissionAttributeModelValidator()
        {
            RuleFor(model => model.Key)
                .NotEmpty()
                .MaximumLength(KeyMaxLength);

            RuleFor(model => model.Value)
                .NotNull()
                .MaximumLength(ValueMaxLength);
        }
    }
}