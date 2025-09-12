using FluentValidation;
using Namezr.Client.Contracts.Auth;
using Namezr.Client.Contracts.Validation;

namespace Namezr.Client.Studio.Questionnaires.Edit;

public class CreateQuestionnaireCommand : ICreatorManagementRequest, IValidatableRequest
{
    public required Guid CreatorId { get; set; }

    public required QuestionnaireEditModel Model { get; set; }

    [RegisterSingleton(typeof(IValidator<CreateQuestionnaireCommand>))]
    internal class Validator : AbstractValidator<CreateQuestionnaireCommand>
    {
        public Validator(IValidator<QuestionnaireEditModel> modelValidator)
        {
            RuleFor(x => x.Model)
                .SetValidator(modelValidator);
        }
    }
}

public class UpdateQuestionnaireCommand : IQuestionnaireManagementRequest, IValidatableRequest
{
    public required Guid QuestionnaireId { get; set; }

    // TODO: convert description to null if empty
    public required QuestionnaireEditModel Model { get; set; }

    [RegisterSingleton(typeof(IValidator<UpdateQuestionnaireCommand>))]
    internal class Validator : AbstractValidator<UpdateQuestionnaireCommand>
    {
        public Validator(IValidator<QuestionnaireEditModel> modelValidator)
        {
            RuleFor(x => x.Model)
                .SetValidator(modelValidator);
        }
    }
}
