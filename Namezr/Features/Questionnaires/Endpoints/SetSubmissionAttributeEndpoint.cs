using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[MapPost(ApiEndpointPaths.SubmissionAttributesSet)]
internal partial class SetSubmissionAttributeEndpoint
{
    private static async ValueTask HandleAsync(
        SetSubmissionAttributeRequest request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        ISubmissionAuditService auditService,
        CancellationToken ct
    )
    {
        HttpContext httpContext = httpContextAccessor.HttpContext!;
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .AsTracking()
            .Include(submission => submission.Version.Questionnaire)
            .SingleOrDefaultAsync(submission => submission.Id == request.SubmissionId, ct);

        if (submission == null) throw new Exception("Bad submission ID");
        await ValidateAccess();

        // Trim and normalize the key
        string normalizedKey = request.Key.Trim();

        // Find existing attribute (case-insensitive)
        SubmissionAttributeEntity? existingAttribute = await dbContext.SubmissionAttributes
            .AsTracking()
            .SingleOrDefaultAsync(
                attr => attr.SubmissionId == request.SubmissionId &&
                        attr.Key.ToLower() == normalizedKey.ToLower(),
                ct
            );

        if (string.IsNullOrEmpty(request.Value))
        {
            // Delete attribute if value is empty
            if (existingAttribute != null)
            {
                // Log deletion before removing
                dbContext.SubmissionHistoryEntries.Add(auditService.AttributeDeleted(
                    submission, normalizedKey, existingAttribute.Value
                ));

                dbContext.SubmissionAttributes.Remove(existingAttribute);
            }
        }
        else
        {
            if (existingAttribute != null)
            {
                // Update existing attribute
                string previousValue = existingAttribute.Value;
                existingAttribute.Value = request.Value;

                // Only log if value actually changed
                if (previousValue != request.Value)
                {
                    dbContext.SubmissionHistoryEntries.Add(auditService.AttributeUpdated(
                        submission, normalizedKey, request.Value, previousValue
                    ));
                }
            }
            else
            {
                // Create new attribute
                SubmissionAttributeEntity newAttribute = new()
                {
                    SubmissionId = request.SubmissionId,
                    Key = normalizedKey,
                    Value = request.Value
                };
                dbContext.SubmissionAttributes.Add(newAttribute);

                // Log creation
                dbContext.SubmissionHistoryEntries.Add(auditService.AttributeUpdated(
                    submission, normalizedKey, request.Value, null
                ));
            }
        }

        await dbContext.SaveChangesAsync(ct);
        return;

        async Task ValidateAccess()
        {
            Guid userId = userAccessor.GetRequiredUserId(httpContext);

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