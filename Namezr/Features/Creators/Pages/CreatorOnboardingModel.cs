using FluentValidation;
using Namezr.Client.Contracts.Validation;

namespace Namezr.Features.Creators.Pages;

internal class CreatorOnboardingModel : IValidatableRequest
{
    public string? CreatorName { get; set; }

    [RegisterSingleton(typeof(IValidator<CreatorOnboardingModel>))]
    public class Validator : AbstractValidator<CreatorOnboardingModel>
    {
        public Validator()
        {
            RuleFor(x => x.CreatorName)
                .NotEmpty();
        }
    }
}