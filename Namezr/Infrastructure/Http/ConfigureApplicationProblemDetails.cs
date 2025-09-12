using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Namezr.Infrastructure.Auth;

namespace Namezr.Infrastructure.Http;

[RegisterSingleton]
public class ConfigureApplicationProblemDetails : IConfigureOptions<ProblemDetailsOptions>
{
    public void Configure(ProblemDetailsOptions options)
    {
        if (options.CustomizeProblemDetails is not null)
        {
            throw new InvalidOperationException($"Multiple set attempts for {nameof(options.CustomizeProblemDetails)}");
        }

        options.CustomizeProblemDetails = c =>
        {
            if (c.Exception is null)
                return;

            c.ProblemDetails = c.Exception switch
            {
                ValidationException ex => new ValidationProblemDetails(
                    ex
                        .Errors
                        .GroupBy(x => x.PropertyName, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Select(e => e.ErrorMessage).ToArray(),
                            StringComparer.OrdinalIgnoreCase
                        )
                )
                {
                    Status = StatusCodes.Status400BadRequest,
                },

                AuthorizationFailedException ex => new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                },

                // other exception handling as desired goes here

                _ => new ProblemDetails
                {
                    Detail = "An error has occurred.",
                    Status = StatusCodes.Status500InternalServerError,
                },
            };

            c.HttpContext.Response.StatusCode = c.ProblemDetails.Status ?? StatusCodes.Status500InternalServerError;
        };
    }
}
