using FluentValidation;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Pages;

public class StudioSubmissionCommentModel
{
    public string Content { get; set; } = string.Empty;
    public StudioSubmissionCommentType Type { get; set; }

    [RegisterSingleton(typeof(IValidator<StudioSubmissionCommentModel>))]
    public class Validator : AbstractValidator<StudioSubmissionCommentModel>
    {
        public Validator()
        {
            RuleFor(model => model.Content)
                .NotEmpty()
                .MaximumLength(SubmissionHistoryEntryEntity.CommentContentMaxLength);
        }
    }
}

public enum StudioSubmissionCommentType
{
    InternalNote,
    PublicComment,
}