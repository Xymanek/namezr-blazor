using FluentValidation;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.SubmissionAttributeUpdate)]
internal partial class UpdateSubmissionAttributeEndpoint
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
        IdentityUserAccessor userAccessor,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        ISubmissionAuditService auditService,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .AsTracking()
            .Include(x => x.Attributes)
            .SingleOrDefaultAsync(x => x.Id == request.SubmissionId, ct);

        if (submission == null)
        {
            return Results.NotFound();
        }

        await ValidateAccess();

        SubmissionAttributeEntity? existingAttribute = submission.Attributes!
            .FirstOrDefault(x => x.Key == request.Key);

        if (existingAttribute != null)
        {
            existingAttribute.Value = request.Value;
        }
        else
        {
            submission.Attributes!.Add(new SubmissionAttributeEntity
            {
                Key = request.Key,
                Value = request.Value
            });
        }

        dbContext.SubmissionHistoryEntries.Add(auditService.AttributeChange(
            submission, request.Key, existingAttribute?.Value ?? string.Empty, request.Value
        ));

        await dbContext.SaveChangesAsync(ct);
        return Results.Ok();

        async Task ValidateAccess()
        {
            Guid userId = userAccessor.GetRequiredUserId(httpContextAccessor.HttpContext!);

            // ReSharper disable once AccessToDisposedClosure
            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(staff =>
                    staff.UserId == userId &&
                    staff.CreatorId == submission.Version.Questionnaire.CreatorId
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}