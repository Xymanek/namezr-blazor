using FluentValidation;
using Namezr.Client.Shared;

namespace Namezr.Client.Studio.Questionnaires.CannedCommentsManagement;

public class CannedCommentSaveRequest
{
    public required Guid CreatorId { get; init; }
    public required CannedCommentModel CannedComment { get; init; }

    [RegisterSingleton(typeof(IValidator<CannedCommentSaveRequest>))]
    public class Validator : AbstractValidator<CannedCommentSaveRequest>
    {
        public Validator(IValidator<CannedCommentModel> cannedCommentValidator)
        {
            RuleFor(x => x.CreatorId)
                .NotEmpty();

            RuleFor(x => x.CannedComment)
                .SetValidator(cannedCommentValidator);
        }
    }
}