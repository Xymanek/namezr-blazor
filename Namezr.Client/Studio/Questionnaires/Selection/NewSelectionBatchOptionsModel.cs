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

    /// <summary>
    /// Filter submissions by attribute key-value pairs. Dictionary keys are attribute keys, values are the required attribute values.
    /// Only submissions that have attributes matching ALL specified key-value pairs will be included.
    /// </summary>
    public Dictionary<string, string> RequiredAttributes { get; set; } = new();

    /// <summary>
    /// Filter out submissions that have any of these attribute key-value pairs.
    /// Dictionary keys are attribute keys, values are the attribute values to exclude.
    /// </summary>
    public Dictionary<string, string> ExcludedAttributes { get; set; } = new();
    
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