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
/// Retrieves filtered attribute values for a specific key from all submissions within the same questionnaire.
/// Used to provide autosuggest functionality for submission attribute value input.
/// </summary>
[Handler]
[MapPost(ApiEndpointPaths.SubmissionAttributesValues)]
internal partial class GetSubmissionAttributeValuesEndpoint
{
    private static async ValueTask<GetSubmissionAttributeValuesResponse> HandleAsync(
        GetSubmissionAttributeValuesRequest request,
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

        // Get all attribute values for the specific key from all submissions in the same questionnaire
        IQueryable<string> valuesQuery = dbContext.SubmissionAttributes
            .Where(attr => 
                attr.Submission.Version.QuestionnaireId == questionnaireId &&
                string.Equals(attr.Key, request.Key, StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(attr.Value))
            .Select(attr => attr.Value)
            .Distinct();

        // Apply user input filtering on server side
        string searchText = request.UserInput?.Trim() ?? "";
        if (!string.IsNullOrEmpty(searchText))
        {
            valuesQuery = valuesQuery.Where(value => 
                EF.Functions.ILike(value, $"%{searchText}%"));
        }

        // Apply ordering and limiting on server side
        List<string> filteredValues = await valuesQuery
            .OrderBy(value => value)
            .Take(10)
            .ToListAsync(ct);

        return new GetSubmissionAttributeValuesResponse
        {
            Values = filteredValues
        };

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