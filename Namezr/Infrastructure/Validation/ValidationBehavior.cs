using FluentValidation;
using FluentValidation.Results;
using Immediate.Handlers.Shared;
using Namezr.Client.Contracts.Validation;

namespace Namezr.Infrastructure.Validation;

[AutoConstructor]
public sealed partial class ValidationBehavior<TRequest, TResponse> : Behavior<TRequest, TResponse>
    where TRequest : IValidatableRequest
{
    private readonly IValidator<TRequest> _validator;

    public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
    {
        ValidationContext<TRequest> context = new(request);
        ValidationResult result = await _validator.ValidateAsync(context, cancellationToken);

        List<ValidationFailure> failures = result.Errors
            .Where(x => x is not null)
            .ToList();

        if (failures.Any())
        {
            Throw();
        }

        return await Next(request, cancellationToken).ConfigureAwait(false);

        void Throw()
        {
            throw new ValidationException(failures);
        }
    }
}
