using FluentValidation;

namespace Namezr.Features.Questionnaires.Pages;

public class NewSelectionBatchOptionsModel
{
    public bool AllowRestarts { get; set; }
    public bool ForceRecalculateEligibility { get; set; }

    public int NumberOfEntriesToSelect { get; set; }
    
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