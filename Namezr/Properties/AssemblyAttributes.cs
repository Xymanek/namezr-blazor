using Immediate.Handlers.Shared;
using Namezr.Infrastructure.Auth;
using Namezr.Infrastructure.Validation;

[assembly: Behaviors(
    typeof(ValidationBehavior<,>),
    typeof(AuthorizationBehaviour<,>)
)]
