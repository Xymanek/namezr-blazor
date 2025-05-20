using System.IO.Compression;
using FluentValidation;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Files.Services;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapGet(ApiEndpointPaths.QuestionnaireSubmissionsBulkDownloadFiles)]
internal partial class BulkDownloadFilesEndpoint
{
    public class Request
    {
        public required Guid QuestionnaireId { get; init; }
        public required Guid FieldId { get; init; }
        public required int[] SubmissionNumbers { get; init; }

        [RegisterSingleton(typeof(IValidator<Request>))]
        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.QuestionnaireId)
                    .NotEmpty()
                    .WithMessage("Questionnaire ID is required");

                RuleFor(x => x.FieldId)
                    .NotEmpty()
                    .WithMessage("Field ID is required");

                RuleFor(x => x.SubmissionNumbers)
                    .NotEmpty()
                    .WithMessage("At least one submission number must be provided");
            }
        }
    }

    private static async ValueTask<IResult> HandleAsync(
        [AsParameters] Request request,
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

        IEnumerable<int> submissionNumbers = request.SubmissionNumbers;
        QuestionnaireSubmissionEntity[] submissions = await dbContext.QuestionnaireSubmissions
            .Include(submission => submission.FieldValues)
            .Include(submission => submission.User)
            .Where(submission =>
                submission.Version.QuestionnaireId == request.QuestionnaireId &&
                submissionNumbers.Contains(submission.Number)
            )
            .ToArrayAsync(ct);

        if (submissions.Length != request.SubmissionNumbers.Distinct().Count())
        {
            return Results.BadRequest("Invalid submission IDs");
        }

        byte[] outputZipBytes = await BuildZipBytes();

        // TODO: audit

        return TypedResults.File(outputZipBytes, "application/zip", "submissions.zip");

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

        // TODO: somehow limit this to prevent DOS attacks
        async Task<byte[]> BuildZipBytes()
        {
            using MemoryStream memoryStream = new();
            using ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true);

            foreach (QuestionnaireSubmissionEntity submission in submissions)
            {
                string? valueSerialized = submission.FieldValues!
                    .SingleOrDefault(fieldValue => fieldValue.FieldId == request.FieldId)?
                    .ValueSerialized;

                if (valueSerialized == null) continue;

                SubmissionValueModel value = fieldValueSerializer
                    .Deserialize(QuestionnaireFieldType.FileUpload, valueSerialized);

                string folderName = GetArchiveFolderName(submission);

                foreach (SubmissionFileData fileData in value.FileValue ?? [])
                {
                    ZipArchiveEntry entry = archive.CreateEntry(folderName + "/" + fileData.Name);

                    await using FileStream fileStream = fileStorageService.OpenRead(fileData.Id);
                    await using Stream entrySteam = entry.Open();

                    await fileStream.CopyToAsync(entrySteam, ct);
                }
            }

            return memoryStream.ToArray();
        }
    }

    private static string GetArchiveFolderName(QuestionnaireSubmissionEntity submission)
    {
        // TODO: sanitize the user name
        return $"{submission.Number} - {submission.User.UserName}";
    }
}