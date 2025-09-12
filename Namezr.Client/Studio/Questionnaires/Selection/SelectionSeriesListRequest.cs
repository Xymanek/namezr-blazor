using FluentValidation;
using Namezr.Client.Contracts.Auth;

namespace Namezr.Client.Studio.Questionnaires.Selection;

public class SelectionSeriesListRequest : IQuestionnaireManagementRequest
{
    public required Guid QuestionnaireId { get; init; }

    [RegisterSingleton(typeof(IValidator<SelectionSeriesListRequest>))]
    public class Validator : AbstractValidator<SelectionSeriesListRequest>
    {
        public Validator()
        {
            RuleFor(x => x.QuestionnaireId)
                .NotEqual(Guid.Empty);
        }
    }
}