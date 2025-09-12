using FluentValidation;
using Namezr.Client.Contracts.Validation;
using Namezr.Features.Questionnaires.Data;

namespace Namezr.Features.Questionnaires.Pages;

public class SubmissionCreateSubmitterCommentModel : IValidatableRequest
{
    public string Content { get; set; } = string.Empty;

    [RegisterSingleton(typeof(IValidator<SubmissionCreateSubmitterCommentModel>))]
    public class Validator : AbstractValidator<SubmissionCreateSubmitterCommentModel>
    {
        public Validator()
        {
            RuleFor(model => model.Content)
                .NotEmpty()
                .MaximumLength(SubmissionHistoryEntryEntity.CommentContentMaxLength);
        }
    }

}