using FluentValidation;
using Namezr.Client.Contracts.Validation;

namespace Namezr.Features.Questionnaires.Pages;

public class NewSeriesModel : IValidatableRequest
{
    public string Name { get; set; } = string.Empty;
    
    [RegisterSingleton(typeof(IValidator<NewSeriesModel>))]
    public class Validator : AbstractValidator<NewSeriesModel>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty();
        }
    }
}