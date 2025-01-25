using FluentValidation;
using vNext.BlazorComponents.FluentValidation;
using IValidatorFactory = vNext.BlazorComponents.FluentValidation.IValidatorFactory;

namespace Namezr.Client.Public.Questionnaires;

/// <summary>
/// See explanation in <see cref="P:Namezr.Client.Public.Questionnaires.SubmissionEditor.FieldValidatorProviders"/>
/// </summary>
internal class FieldValidatorProvider : IValidatorFactory
{
    public required IValidator<SubmissionValueModel> Validator { private get; init; }
    public required SubmissionValueModel Model { private get; init; }

    public IValidator? CreateValidator(ValidatorFactoryContext context)
    {
        if (context.Model == Model)
        {
            return Validator;
        }
        
        return null;
    }
}