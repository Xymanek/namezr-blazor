using FluentValidation;

namespace Namezr.Client.Public.Questionnaires;

public class SubmissionCreateModel
{
    // TODO: this needs to be encrypted by server
    public required Guid QuestionnaireVersionId { get; set; }
    
    public Guid? SubmissionId { get; set; }

    public required Dictionary<Guid, SubmissionValueModel> Values { get; set; }

    public required List<string> NewFileTickets { get; init; }

    [RegisterSingleton(typeof(IValidator<SubmissionCreateModel>))]
    public class Validator : AbstractValidator<SubmissionCreateModel>
    {
        public Validator()
        {
            RuleFor(x => x.Values)
                .NotEmpty();
        }
    }
}