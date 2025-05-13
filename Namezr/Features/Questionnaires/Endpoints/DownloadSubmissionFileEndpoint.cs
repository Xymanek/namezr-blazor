using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Files.Services;
using Namezr.Features.Identity.Data;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Behaviors] // Remove the global validation behavior
[Authorize]
[MapGet(ApiEndpointPaths.QuestionnaireSubmissionDownloadFile)]
internal partial class DownloadSubmissionFileEndpoint
{
    internal class Parameters
    {
        public required Guid SubmissionId { get; init; }
        public required Guid FileId { get; init; }
    }

    private static async ValueTask<IResult> Handle(
        [AsParameters] Parameters parameters,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IFieldValueSerializer fieldValueSerializer,
        IHttpContextAccessor httpContextAccessor,
        IFileStorageService fileStorageService,
        IdentityUserAccessor userAccessor,
        IDownloadContentTypeProvider contentTypeProvider,
        ISubmissionAuditService submissionAudit,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .AsNoTracking()
            .Include(x => x.FieldValues!.Where(value => value.Field.Type == QuestionnaireFieldType.FileUpload))
            .Include(x => x.Version.Questionnaire)
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == parameters.SubmissionId, ct);

        // TODO: return 404 if not found
        if (submission is null)
        {
            throw new Exception("Submission not found");
        }

        ApplicationUser user = await userAccessor.GetRequiredUserAsync(httpContextAccessor.HttpContext!);
        bool isOwnSubmission = user.Id == submission.UserId;

        await ValidateAccess();

        (QuestionnaireFieldValueEntity fieldValue, SubmissionFileData fileData) = submission.FieldValues!
            .SelectMany(fieldValue =>
            {
                SubmissionValueModel value = fieldValueSerializer.Deserialize(
                    QuestionnaireFieldType.FileUpload, // Safe assumption due to .Include() filter above
                    fieldValue.ValueSerialized
                );

                return (value.FileValue ?? [])
                    .Select(fileData => (fieldValue, fileData));
            })
            .SingleOrDefault(tuple => tuple.fileData.Id == parameters.FileId);

        // TODO: return 404 if not found
        if (fileData is null)
        {
            throw new Exception("File not found");
        }

        // TODO: this should be passed from the client and hooked up to the access check
        if (isOwnSubmission)
        {
            await submissionAudit.DownloadFileSubmitter(submission, fieldValue, fileData, inBatch: false, ct);
        }
        else
        {
            await submissionAudit.DownloadFileStaff(submission, fieldValue, fileData, inBatch: false, ct);
        }

        return Results.File(
            fileStorageService.GetFilePath(fileData.Id),
            contentType: contentTypeProvider.MaybeGetFromFilename(fileData.Name),
            fileDownloadName: fileData.Name
        );

        async Task ValidateAccess()
        {
            // Can always download own files
            if (isOwnSubmission) return;

            // ReSharper disable once AccessToDisposedClosure
            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(staff =>
                    staff.UserId == user.Id &&
                    staff.CreatorId == submission.Version.Questionnaire.CreatorId
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}