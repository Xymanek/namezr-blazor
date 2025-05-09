using FluentValidation;
using Namezr.Client.Shared;

namespace Namezr.Client.Studio.Questionnaires.LabelsManagement;

public class LabelSaveRequest
{
    public required Guid CreatorId { get; init; }
    public required SubmissionLabelModel Label { get; init; }

    [RegisterSingleton(typeof(IValidator<LabelSaveRequest>))]
    public class Validator : AbstractValidator<LabelSaveRequest>
    {
        public Validator(IValidator<SubmissionLabelModel> labelValidator)
        {
            RuleFor(x => x.CreatorId)
                .NotEmpty();

            RuleFor(x => x.Label)
                .SetValidator(labelValidator);
        }
    }
}