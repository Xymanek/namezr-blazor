using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

/// <summary>
/// Retrieves distinct attribute keys from all submissions within the same questionnaire.
/// Used to provide autosuggest functionality for submission attribute key input.
/// </summary>
[Handler]
[MapGet(ApiEndpointPaths.SubmissionAttributesKeys)]
internal partial class GetSubmissionAttributeKeysEndpoint
{
    private static async ValueTask<GetSubmissionAttributeKeysResponse> HandleAsync(
        [AsParameters] GetSubmissionAttributeKeysRequest request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken ct
    )
    {
        HttpContext httpContext = httpContextAccessor.HttpContext!;
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        // Get the submission to validate access and get questionnaire context
        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .Include(submission => submission.Version.Questionnaire)
            .SingleOrDefaultAsync(submission => submission.Id == request.SubmissionId, ct);

        if (submission == null) throw new Exception("Bad submission ID");
        await ValidateAccess();

        // Get the questionnaire ID from the submission
        Guid questionnaireId = submission.Version.QuestionnaireId;

        // Get distinct attribute keys from all submissions in the same questionnaire
        List<string> distinctKeys = await dbContext.SubmissionAttributes
            .Where(attr => attr.Submission.Version.QuestionnaireId == questionnaireId)
            .Select(attr => attr.Key)
            .Distinct()
            .OrderBy(key => key)
            .ToListAsync(ct);

        return new GetSubmissionAttributeKeysResponse
        {
            Keys = distinctKeys
        };

        /// <summary>
        /// Validates that the current user has permission to access submission attributes
        /// for the questionnaire associated with the specified submission.
        /// </summary>
        /// <exception cref="Exception">Thrown when the user lacks creator staff permissions</exception>
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