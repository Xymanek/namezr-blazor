using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
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

        // Find existing attribute
        SubmissionAttributeEntity? existingAttribute = await dbContext.SubmissionAttributes
            .AsTracking()
            .SingleOrDefaultAsync(
                attr => attr.SubmissionId == request.SubmissionId && attr.Key == request.Key,
                ct
            );

        if (string.IsNullOrEmpty(request.Value))
        {
            // Delete attribute if value is empty
            if (existingAttribute != null)
            {
                dbContext.SubmissionAttributes.Remove(existingAttribute);
            }
        }
        else
        {
            if (existingAttribute != null)
            {
                // Update existing attribute
                existingAttribute.Value = request.Value;
            }
            else
            {
                // Create new attribute
                SubmissionAttributeEntity newAttribute = new()
                {
                    SubmissionId = request.SubmissionId,
                    Key = request.Key,
                    Value = request.Value
                };
                dbContext.SubmissionAttributes.Add(newAttribute);
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