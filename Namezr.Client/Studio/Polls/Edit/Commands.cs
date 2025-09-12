using FluentValidation;
using Namezr.Client.Contracts.Auth;
using Namezr.Client.Contracts.Validation;

namespace Namezr.Client.Studio.Polls.Edit;

public class CreatePollCommand : ICreatorManagementRequest, IValidatableRequest
{
    public required Guid CreatorId { get; init; }

    public required PollEditModel Model { get; init; }

    [RegisterSingleton(typeof(IValidator<CreatePollCommand>))]
    internal class Validator : AbstractValidator<CreatePollCommand>
    {
        public Validator(IValidator<PollEditModel> modelValidator)
        {
            RuleFor(x => x.Model)
                .SetValidator(modelValidator);
        }
    }
}

public class UpdatePollCommand : IPollManagementRequest, IValidatableRequest
{
    public required Guid PollId { get; init; }

    // TODO: convert description to null if empty
    public required PollEditModel Model { get; init; }

    [RegisterSingleton(typeof(IValidator<UpdatePollCommand>))]
    internal class Validator : AbstractValidator<UpdatePollCommand>
    {
        public Validator(IValidator<PollEditModel> modelValidator)
        {
            RuleFor(x => x.Model)
                .SetValidator(modelValidator);
        }
    }
}