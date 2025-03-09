using FluentValidation;
using Namezr.Client.Types;

namespace Namezr.Client.Studio.Questionnaires.Edit;

public class QuestionnaireEditModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public QuestionnaireApprovalMode ApprovalMode { get; set; }

    public List<EligibilityOptionEditModel> EligibilityOptions { get; set; } = new();
    public List<QuestionnaireFieldEditModel> Fields { get; set; } = new();

    public void AddBlankField()
    {
        Fields.Add(new QuestionnaireFieldEditModel
        {
            Id = Guid.CreateVersion7(),
        });
    }

    [RegisterSingleton(typeof(IValidator<QuestionnaireEditModel>))]
    internal sealed class Validator : AbstractValidator<QuestionnaireEditModel>
    {
        public Validator(
            IValidator<QuestionnaireFieldEditModel> fieldValidator,
            IValidator<EligibilityOptionEditModel> eligibilityOptionValidator
        )
        {
            RuleFor(x => x.Title)
                .MinimumLength(3)
                .MaximumLength(TitleMaxLength);

            RuleFor(x => x.Description)
                .MaximumLength(DescriptionMaxLength);

            RuleFor(x => x.Fields)
                .NotEmpty();

            RuleForEach(x => x.Fields)
                .SetValidator(fieldValidator);

            RuleFor(x => x.EligibilityOptions)
                .Must(options => options.Select(x => x.PlanId).Distinct().Count() == options.Count)
                .WithMessage("Cannot select the same support plan multiple times");

            RuleForEach(x => x.EligibilityOptions)
                .SetValidator(eligibilityOptionValidator);
        }
    }

    public const int TitleMaxLength = 50;
    public const int DescriptionMaxLength = 1000;
}

public class EligibilityOptionEditModel
{
    public EligibilityPlanId? PlanId { get; set; }

    public string PriorityGroup { get; set; } = string.Empty;
    public decimal PriorityModifier { get; set; } = 1;

    [RegisterSingleton(typeof(IValidator<EligibilityOptionEditModel>))]
    internal sealed class Validator : AbstractValidator<EligibilityOptionEditModel>
    {
        public Validator()
        {
            RuleFor(x => x.PlanId)
                .NotNull();

            RuleFor(x => x.PriorityGroup)
                .MaximumLength(PriorityGroupMaxLength);

            RuleFor(x => x.PriorityModifier)
                .GreaterThan(0);
        }
    }

    public const int PriorityGroupMaxLength = 50;
}

public class QuestionnaireFieldEditModel
{
    public required Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public QuestionnaireFieldType? Type { get; set; }

    public QuestionnaireTextFieldOptionsModel? TextOptions { get; set; }
    public QuestionnaireNumberFieldOptionsModel? NumberOptions { get; set; }
    public QuestionnaireFileUploadFieldOptionsModel? FileUploadOptions { get; set; }

    [RegisterSingleton(typeof(IValidator<QuestionnaireFieldEditModel>))]
    internal sealed class Validator : AbstractValidator<QuestionnaireFieldEditModel>
    {
        public Validator(
            IValidator<QuestionnaireTextFieldOptionsModel> textOptionsValidator,
            IValidator<QuestionnaireNumberFieldOptionsModel> numberOptionsValidator,
            IValidator<QuestionnaireFileUploadFieldOptionsModel> fileUploadOptionsValidator
        )
        {
            RuleFor(x => x.Title)
                .MinimumLength(3)
                .MaximumLength(TitleMaxLength);

            RuleFor(x => x.Description)
                .MaximumLength(DescriptionMaxLength);

            RuleFor(x => x.Type)
                .NotNull();

            RuleFor(x => x.TextOptions)
                .SetValidator(textOptionsValidator!);

            RuleFor(x => x.NumberOptions)
                .SetValidator(numberOptionsValidator!);

            RuleFor(x => x.FileUploadOptions)
                .SetValidator(fileUploadOptionsValidator!);
        }
    }

    public const int TitleMaxLength = 50;
    public const int DescriptionMaxLength = 1000;
}

public class QuestionnaireTextFieldOptionsModel
{
    public bool IsMultiline { get; set; }

    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }

    [RegisterSingleton(typeof(IValidator<QuestionnaireTextFieldOptionsModel>))]
    internal sealed class Validator : AbstractValidator<QuestionnaireTextFieldOptionsModel>
    {
        public Validator()
        {
            RuleFor(x => x.MinLength)
                .LessThanOrEqualTo(x => x.MaxLength)
                .When(x => x.MinLength is not null && x.MaxLength is not null);

            RuleFor(x => x.MinLength)
                .GreaterThan(0);

            RuleFor(x => x.MaxLength)
                .GreaterThan(0);
        }
    }
}

public class QuestionnaireNumberFieldOptionsModel
{
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    [RegisterSingleton(typeof(IValidator<QuestionnaireNumberFieldOptionsModel>))]
    internal sealed class Validator : AbstractValidator<QuestionnaireNumberFieldOptionsModel>
    {
        public Validator()
        {
            RuleFor(x => x.MinValue)
                .LessThanOrEqualTo(x => x.MaxValue)
                .When(x => x.MinValue is not null && x.MaxValue is not null);
        }
    }
}

public class QuestionnaireFileUploadFieldOptionsModel
{
    /// <summary>
    /// If empty - any extension
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new();

    public int MinItemCount { get; set; } = 0;
    public int MaxItemCount { get; set; } = 1;

    public long? MinItemSizeBytes { get; set; }
    public long? MaxItemSizeBytes { get; set; }

    [RegisterSingleton(typeof(IValidator<QuestionnaireFileUploadFieldOptionsModel>))]
    internal sealed class Validator : AbstractValidator<QuestionnaireFileUploadFieldOptionsModel>
    {
        public Validator()
        {
            RuleFor(x => x.MinItemSizeBytes)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.MaxItemSizeBytes)
                .GreaterThan(1)
                .GreaterThanOrEqualTo(x => x.MinItemSizeBytes);

            RuleFor(x => x.MinItemCount)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.MaxItemCount)
                .GreaterThan(1)
                .GreaterThanOrEqualTo(x => x.MinItemCount);
        }
    }
}

public enum QuestionnaireFieldType
{
    Text,
    Number,
    FileUpload,
}

public enum QuestionnaireApprovalMode
{
    NotRequired,
    KeepApprovedWhilePending,
    DeactivateWhilePending,
}