using FluentValidation;
using Namezr.Client.Shared;

namespace Namezr.Client.Studio.Questionnaires;

public record SetSubmissionAttributeRequest
{
    public required Guid SubmissionId { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; } // Empty value means delete

    [RegisterSingleton(typeof(IValidator<SetSubmissionAttributeRequest>))]
    public class Validator : AbstractValidator<SetSubmissionAttributeRequest>
    {
        public Validator()
        {
            RuleFor(x => x.SubmissionId)
                .NotEmpty();

            RuleFor(x => x.Key)
                .NotEmpty()
                .MaximumLength(SubmissionAttributeModel.KeyMaxLength);

            RuleFor(x => x.Value)
                .NotNull()
                .MaximumLength(SubmissionAttributeModel.ValueMaxLength);
        }
    }
}