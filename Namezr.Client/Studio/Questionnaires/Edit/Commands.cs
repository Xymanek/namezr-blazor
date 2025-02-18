using FluentValidation;

namespace Namezr.Client.Studio.Questionnaires.Edit;

public class CreateQuestionnaireCommand
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

public class UpdateQuestionnaireCommand
{
    public required Guid Id { get; set; }

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