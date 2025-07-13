using FluentValidation;

namespace Namezr.Client.Studio.Questionnaires;

public record GetSubmissionAttributeKeysRequest
{
    public required Guid SubmissionId { get; init; }

    [RegisterSingleton(typeof(IValidator<GetSubmissionAttributeKeysRequest>))]
    public class Validator : AbstractValidator<GetSubmissionAttributeKeysRequest>
    {
        public Validator()
        {
            RuleFor(x => x.SubmissionId)
                .NotEmpty();
        }
    }
}