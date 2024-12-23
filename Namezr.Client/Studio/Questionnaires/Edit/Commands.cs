using FluentValidation;

namespace Namezr.Client.Studio.Questionnaires.Edit;

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