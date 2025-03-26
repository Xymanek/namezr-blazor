using FluentValidation;
using Namezr.Client.Studio.Questionnaires.Edit;

namespace Namezr.Client.Public.Questionnaires;

public class SubmissionValueModel
{
    public string StringValue { get; set; } = string.Empty;
    public decimal? NumberValue { get; set; }
    
    /// <summary>
    /// File IDs that are selected
    /// </summary>
    public List<SubmissionFileData>? FileValue { get; set; }
}

public record SubmissionFileData
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required long SizeBytes { get; init; }
}

/// <typeparam name="TKey">
/// Usually <see cref="Guid"/> but <see cref="string"/> when validating in form context.
/// </typeparam>
public class SubmissionValuesValidator<TKey> : AbstractValidator<Dictionary<TKey, SubmissionValueModel>>
    where TKey : notnull
{
    public SubmissionValuesValidator(QuestionnaireConfigModel config, Func<Guid, TKey> keySelector)
    {
        Dictionary<TKey, IValidator<SubmissionValueModel>> fieldValidatorMap = new();

        foreach (QuestionnaireConfigFieldModel fieldConfig in config.Fields)
        {
            IValidator<SubmissionValueModel>? validator = fieldConfig.Type switch
            {
                QuestionnaireFieldType.Text => new SubmissionValueStringValidator(fieldConfig.TextOptions!),
                QuestionnaireFieldType.Number => new SubmissionValueNumberValidator(fieldConfig.NumberOptions!),

                // TODO: File upload validation is handled separately in the UI and the API
                QuestionnaireFieldType.FileUpload => null,

                _ => throw new ArgumentOutOfRangeException(),
            };

            if (validator is null) continue;

            TKey key = keySelector(fieldConfig.Id);

            fieldValidatorMap.Add(key, validator);
            RuleFor(x => x[key]).SetValidator(validator);
        }

        PerFieldValidators = fieldValidatorMap;
    }

    public IReadOnlyDictionary<TKey, IValidator<SubmissionValueModel>> PerFieldValidators { get; }
}

internal class SubmissionValueBaseValidator : AbstractValidator<SubmissionValueModel>
{
    protected SubmissionValueBaseValidator(QuestionnaireFieldType fieldType)
    {
        if (fieldType != QuestionnaireFieldType.Text)
        {
            RuleFor(x => x.StringValue)
                .Empty();
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