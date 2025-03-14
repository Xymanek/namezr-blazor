using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Files.Services;
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
        IFileStorageService fileStorageService,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .AsNoTracking()
            .Include(x => x.FieldValues!.Where(value => value.Field.Type == QuestionnaireFieldType.FileUpload))
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == parameters.SubmissionId, ct);

        // TODO: return 404 if not found
        if (submission is null)
        {
            throw new Exception("Submission not found");
        }
        
        // TODO: validate access to submission

        SubmissionFileData? fileData = submission.FieldValues!
            .Select(v => fieldValueSerializer.Deserialize(QuestionnaireFieldType.FileUpload, v.ValueSerialized))
            .SelectMany(v => v.FileValue!)
            .SingleOrDefault(file => file.Id == parameters.FileId);

        // TODO: return 404 if not found
        if (fileData is null)
        {
            throw new Exception("File not found");
        }

        return Results.File(
            fileStorageService.GetFilePath(fileData.Id),
            fileDownloadName: fileData.Name
        );
    }
}