using FluentValidation;

namespace Namezr.Client.Studio.Questionnaires;

public record GetSubmissionAttributeValuesRequest
{
    public required Guid SubmissionId { get; init; }
    public required string Key { get; init; }
    public required string UserInput { get; init; }

    [RegisterSingleton(typeof(IValidator<GetSubmissionAttributeValuesRequest>))]
    public class Validator : AbstractValidator<GetSubmissionAttributeValuesRequest>
    {
        public Validator()
        {
            RuleFor(x => x.SubmissionId)
                .NotEmpty();
            
            RuleFor(x => x.Key)
                .NotEmpty()
                .MaximumLength(250);
            
            RuleFor(x => x.UserInput)
                .MaximumLength(5000); // Allow empty but limit length if provided
        }
    }
}