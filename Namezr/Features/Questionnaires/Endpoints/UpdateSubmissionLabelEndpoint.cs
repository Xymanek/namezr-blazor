using FluentValidation;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.SubmissionAttributeUpdate)]
internal partial class UpdateSubmissionLabelEndpoint
{
    public class Request
    {
        public required Guid SubmissionId { get; init; }

        public required string Key { get; init; }
        public required string Value { get; init; }

        [RegisterSingleton(typeof(IValidator<Request>))]
        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(r => r.Key)
                    .MaximumLength(SubmissionAttributeEntity.KeyMaxLength);

                RuleFor(r => r.Value)
                    .MaximumLength(SubmissionAttributeEntity.ValueMaxLength);
            }
        }
    }

    private static async ValueTask<IResult> HandleAsync(
        Request request,
        IHttpContextAccessor httpContextAccessor,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ISubmissionAuditService auditService,
        CancellationToken ct
    )
    {
        // TODO: fetch submission
        // TODO: validate access
    }
}