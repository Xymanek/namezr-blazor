using FluentValidation;

namespace Namezr.Client.Shared;

public partial class CannedCommentModel
{
    public Guid Id { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public StudioSubmissionCommentType CommentType { get; set; }

    public bool IsActive { get; set; } = true;

    public const int TitleMaxLength = 100;
    public const int ContentMaxLength = 5000;

    [RegisterSingleton(typeof(IValidator<CannedCommentModel>))]
    public partial class CannedCommentModelValidator : AbstractValidator<CannedCommentModel>
    {
        public CannedCommentModelValidator()
        {
            RuleFor(model => model.Title)
                .NotEmpty()
                .MaximumLength(TitleMaxLength);

            RuleFor(model => model.Content)
                .NotEmpty()
                .MaximumLength(ContentMaxLength);

            RuleFor(model => model.CommentType)
                .IsInEnum();
        }
    }
}