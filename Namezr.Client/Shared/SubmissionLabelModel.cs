using System.Text.RegularExpressions;
using FluentValidation;

namespace Namezr.Client.Shared;

public partial class SubmissionLabelModel
{
    public Guid Id { get; set; }

    public string? Text { get; set; }

    /// <summary>
    /// Shown on hover
    /// </summary>
    public string? Description { get; set; }

    public string? Colour { get; set; }

    // public BootstrapIcon? Icon { get; set; }

    public bool IsSubmitterVisible { get; set; }

    public const int TextMaxLength = 40;
    public const int DescriptionMaxLength = 200;

    [RegisterSingleton(typeof(IValidator<SubmissionLabelModel>))]
    public partial class SubmissionLabelModelValidator : AbstractValidator<SubmissionLabelModel>
    {
        public SubmissionLabelModelValidator()
        {
            RuleFor(model => model.Text)
                .NotEmpty()
                .MaximumLength(TextMaxLength);

            RuleFor(model => model.Colour)
                .NotEmpty()
                .Matches(GetColourRegex());
        }

        [GeneratedRegex("#[0-9a-fA-F]{6}")]
        private partial Regex GetColourRegex();
    }
}