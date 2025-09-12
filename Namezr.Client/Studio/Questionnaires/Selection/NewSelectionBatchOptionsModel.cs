using FluentValidation;
using Namezr.Client.Contracts.Validation;

namespace Namezr.Client.Studio.Questionnaires.Selection;

public class NewSelectionBatchOptionsModel : IValidatableRequest
{
    public bool AllowRestarts { get; set; }
    public bool ForceRecalculateEligibility { get; set; }

    public int NumberOfEntriesToSelect { get; set; }

    public List<Guid> IncludedLabelIds { get; set; } = [];
    public List<Guid> ExcludedLabelIds { get; set; } = [];
    
    [RegisterSingleton(typeof(IValidator<NewSelectionBatchOptionsModel>))]
    public class Validator : AbstractValidator<NewSelectionBatchOptionsModel>
    {
        public Validator()
        {
            RuleFor(x => x.NumberOfEntriesToSelect)
                .GreaterThan(0);
        }
    }
}