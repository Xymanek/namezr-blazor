using FluentValidation;
using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Client.Public.Questionnaires;

public class SubmissionValueModel
{
    public string StringValue { get; set; } = string.Empty;
    public decimal? NumberValue { get; set; }
}

public class SubmissionModelValidator : AbstractValidator<Dictionary<string, SubmissionValueModel>>
{
    public SubmissionModelValidator(IReadOnlyDictionary<string, IValidator<SubmissionValueModel>> fieldValidatorMap)
    {
        foreach ((string fieldId, IValidator<SubmissionValueModel> validator) in fieldValidatorMap)
        {
            RuleFor(x => x[fieldId]).SetValidator(validator);
        }
    }
    
    public static IReadOnlyDictionary<string, IValidator<SubmissionValueModel>> CreateRuleMap(QuestionnaireConfigModel config)
    {
        Dictionary<string, IValidator<SubmissionValueModel>> fieldValidatorMap = new();
        
        foreach (QuestionnaireConfigFieldModel fieldConfig in config.Fields)
        {
            IValidator<SubmissionValueModel>? validator;
            switch (fieldConfig.Type)
            {
                case QuestionnaireFieldType.Text:
                    validator = new SubmissionValueStringValidator(fieldConfig.TextOptions!);
                    break;

                case QuestionnaireFieldType.Number:
                    validator = new SubmissionValueNumberValidator(fieldConfig.NumberOptions!);
                    break;
                
                case QuestionnaireFieldType.FileUpload:
                    // break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            fieldValidatorMap.Add(fieldConfig.Id.ToString(), validator);
        }
        
        return fieldValidatorMap;
    }
}

internal class SubmissionValueBaseValidator : AbstractValidator<SubmissionValueModel>
{
    protected SubmissionValueBaseValidator(QuestionnaireFieldType fieldType)
    {
        if (fieldType != QuestionnaireFieldType.Text)
        {
            RuleFor(x => x.StringValue)
                .Null();
        }
        
        if (fieldType != QuestionnaireFieldType.Number)
        {
            RuleFor(x => x.NumberValue)
                .Null();
        }
    }
}

internal class SubmissionValueStringValidator : SubmissionValueBaseValidator
{
    public SubmissionValueStringValidator(QuestionnaireTextFieldOptionsModel options)
        : base(QuestionnaireFieldType.Text)
    {
        IRuleBuilderInitial<SubmissionValueModel, string>? ruleBuilder = RuleFor(x => x.StringValue);

        if (options.MinLength is not null)
        {
            ruleBuilder.MinimumLength(options.MinLength.Value);
        }

        if (options.MaxLength is not null)
        {
            ruleBuilder.MaximumLength(options.MaxLength.Value);
        }
    }
}

internal class SubmissionValueNumberValidator : SubmissionValueBaseValidator
{
    public SubmissionValueNumberValidator(QuestionnaireNumberFieldOptionsModel options)
        : base(QuestionnaireFieldType.Number)
    {
        IRuleBuilderInitial<SubmissionValueModel, decimal?> ruleBuilder = RuleFor(x => x.NumberValue);

        if (options.MinValue is not null)
        {
            ruleBuilder.GreaterThanOrEqualTo(options.MinValue.Value);
        }

        if (options.MaxValue is not null)
        {
            ruleBuilder.LessThanOrEqualTo(options.MaxValue.Value);
        }
    }
}