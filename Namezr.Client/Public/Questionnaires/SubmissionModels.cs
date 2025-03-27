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

/// <summary>
/// Information about a file that is available before it is uploaded
/// (and hence before the ID is assigned)
/// </summary>
public record SubmittableFileData
{
    public required string Name { get; init; }
    public required long SizeBytes { get; init; }
}

public record SubmissionFileData : SubmittableFileData
{
    public required Guid Id { get; init; }
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
            IValidator<SubmissionValueModel> validator = fieldConfig.Type switch
            {
                QuestionnaireFieldType.Text => new SubmissionValueStringValidator(fieldConfig.TextOptions!),
                QuestionnaireFieldType.Number => new SubmissionValueNumberValidator(fieldConfig.NumberOptions!),
                QuestionnaireFieldType.FileUpload => new SubmissionValueFileUploadValidator(
                    fieldConfig.FileUploadOptions!
                ),

                _ => throw new ArgumentOutOfRangeException(),
            };

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

        if (fieldType != QuestionnaireFieldType.FileUpload)
        {
            RuleFor(x => x.FileValue)
                .Empty();
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

internal class SubmissionValueFileUploadValidator : SubmissionValueBaseValidator
{
    public SubmissionValueFileUploadValidator(QuestionnaireFileUploadFieldOptionsModel options)
        : base(QuestionnaireFieldType.FileUpload)
    {
        IRuleBuilderInitial<SubmissionValueModel, List<SubmissionFileData>?> collectionRules
            = RuleFor(x => x.FileValue);

        collectionRules
            .NotNull()
            .DependentRules(() =>
            {
                collectionRules
                    .Must(files => files!.Count >= options.MinItemCount)
                    .WithMessage($"At least {options.MinItemCount} files are required");

                collectionRules
                    .Must(files => files!.Count <= options.MaxItemCount)
                    .WithMessage($"At most {options.MaxItemCount} files are allowed");

                RuleForEach(x => x.FileValue)
                    .SetValidator(new SubmittableFileDataValidator(options))
                    .ChildRules(itemRules =>
                    {
                        itemRules.RuleFor(file => file.Id)
                            .NotEmpty();
                    });
            });
    }
}

internal class SubmittableFileDataValidator : AbstractValidator<SubmittableFileData>
{
    public SubmittableFileDataValidator(QuestionnaireFileUploadFieldOptionsModel options)
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        if (options.MinItemSizeBytes is not null)
        {
            RuleFor(x => x.SizeBytes)
                .GreaterThanOrEqualTo(options.MinItemSizeBytes.Value);
        }

        if (options.MaxItemSizeBytes is not null)
        {
            RuleFor(x => x.SizeBytes)
                .LessThanOrEqualTo(options.MaxItemSizeBytes.Value);
        }

        if (options.AllowedExtensions is { Count: > 0 })
        {
            RuleFor(x => x.Name)
                .Must(name => options.AllowedExtensions.Any(ext => name.EndsWith(
                    $".{ext}",
                    StringComparison.InvariantCultureIgnoreCase
                )))
                .WithMessage("Invalid file extension");
        }
    }
}