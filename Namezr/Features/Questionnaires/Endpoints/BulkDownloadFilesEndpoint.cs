using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Files.Services;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.QuestionnaireSubmissionsBulkDownloadFiles)]
internal partial class BulkDownloadFilesEndpoint
{
    public class Request
    {
        public required Guid QuestionnaireId { get; init; }
        public required List<Guid> SubmissionIds { get; init; }
        public required Guid FieldId { get; init; }
    }

    private static async ValueTask<IResult> HandleAsync(
        Request request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IFieldValueSerializer fieldValueSerializer,
        IHttpContextAccessor httpContextAccessor,
        IFileStorageService fileStorageService,
        IdentityUserAccessor userAccessor,
        ISubmissionAuditService submissionAudit,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireEntity? questionnaire = await dbContext.Questionnaires
            .Include(q => q.Fields)
            .SingleOrDefaultAsync(x => x.Id == request.QuestionnaireId, ct);

        if (questionnaire == null)
        {
            return Results.BadRequest("Invalid questionnaire ID");
        }

        await ValidateAccess();

        if (questionnaire.Fields!.All(field => field.Id != request.FieldId))
        {
            return Results.BadRequest("Invalid field ID");
        }

        if (
            questionnaire.Fields!.Single(field => field.Id == request.FieldId).Type
            != QuestionnaireFieldType.FileUpload
        )
        {
            return Results.BadRequest("Not a file field");
        }
        
        QuestionnaireSubmissionEntity[] submissions = await dbContext.QuestionnaireSubmissions
            .Where(submission =>
                submission.Version.QuestionnaireId == request.QuestionnaireId &&
                request.SubmissionIds.Contains(submission.Id)
            )
            .ToArrayAsync(ct);

        if (submissions.Length != request.SubmissionIds.Distinct().Count())
        {
            return Results.BadRequest("Invalid submission IDs");
        }

        // TODO: zip
        
        // TODO: audit
        
        // TODO: serve

        async Task ValidateAccess()
        {
            Guid userId = userAccessor.GetRequiredUserId(httpContextAccessor.HttpContext!);

            // ReSharper disable once AccessToDisposedClosure
            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(staff =>
                    staff.UserId == userId &&
                    staff.CreatorId == questionnaire.CreatorId
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}