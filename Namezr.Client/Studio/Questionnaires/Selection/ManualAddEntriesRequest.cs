using FluentValidation;
using Namezr.Client.Contracts.Auth;

namespace Namezr.Client.Studio.Questionnaires.Selection;

public class ManualAddEntriesRequest : ISeriesManagementRequest
{
    public required Guid SeriesId { get; init; }
    public required Guid[] SubmissionIds { get; init; }

    [RegisterSingleton(typeof(IValidator<ManualAddEntriesRequest>))]
    public class Validator : AbstractValidator<ManualAddEntriesRequest>
    {
        public Validator()
        {
            RuleFor(x => x.SeriesId)
                .NotEqual(Guid.Empty);

            RuleFor(x => x.SubmissionIds)
                .NotNull()
                .NotEmpty()
                .WithMessage("At least one submission must be selected");

            RuleForEach(x => x.SubmissionIds)
                .NotEqual(Guid.Empty);
        }
    }
}