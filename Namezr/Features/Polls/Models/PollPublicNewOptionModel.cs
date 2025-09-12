using FluentValidation;
using Namezr.Client.Contracts.Validation;
using Namezr.Client.Studio.Polls.Edit;

namespace Namezr.Features.Polls.Models;

public class PollPublicNewOptionModel : IValidatableRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [RegisterSingleton(typeof(IValidator<PollPublicNewOptionModel>))]
    internal sealed class Validator : AbstractValidator<PollPublicNewOptionModel>
    {
        public Validator()
        {
            RuleFor(x => x.Title)
                .MinimumLength(PollOptionEditModel.TitleMinLength)
                .MaximumLength(PollOptionEditModel.TitleMaxLength);

            RuleFor(x => x.Description)
                .MaximumLength(PollOptionEditModel.DescriptionMaxLength);
        }
    }
}