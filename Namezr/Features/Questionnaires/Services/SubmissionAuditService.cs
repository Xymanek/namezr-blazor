using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Public.Questionnaires;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Services;

internal interface ISubmissionAuditService
{
    [MustUseReturnValue]
    SubmissionHistoryLabelRemovedEntity LabelRemovedStaff(
        QuestionnaireSubmissionEntity submission,
        SubmissionLabelEntity label
    );

    [MustUseReturnValue]
    SubmissionHistoryLabelAppliedEntity LabelAddedStaff(
        QuestionnaireSubmissionEntity submission,
        SubmissionLabelEntity label
    );

    ValueTask DownloadFileSubmitter(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldValueEntity fieldValue,
        SubmissionFileData file,
        bool inBatch,
        CancellationToken ct
    );

    ValueTask DownloadFileStaff(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldValueEntity fieldValue,
        SubmissionFileData file,
        bool inBatch,
        CancellationToken ct
    );
}

[AutoConstructor]
[RegisterSingleton]
internal partial class SubmissionAuditService : ISubmissionAuditService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SubmissionAuditService> _logger; // TODO: add logs
    private readonly IdentityUserAccessor _userAccessor;
    private readonly IClock _clock;

    public SubmissionHistoryLabelAppliedEntity LabelAddedStaff(
        QuestionnaireSubmissionEntity submission,
        SubmissionLabelEntity label
    )
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        return new SubmissionHistoryLabelAppliedEntity
        {
            Submission = submission,
            OccuredAt = _clock.GetCurrentInstant(),

            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = true,
            InstigatorUserId = userId,

            Label = label,
        };
    }

    public SubmissionHistoryLabelRemovedEntity LabelRemovedStaff(
        QuestionnaireSubmissionEntity submission,
        SubmissionLabelEntity label
    )
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        return new SubmissionHistoryLabelRemovedEntity
        {
            Submission = submission,
            OccuredAt = _clock.GetCurrentInstant(),

            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = true,
            InstigatorUserId = userId,

            Label = label,
        };
    }

    public async ValueTask DownloadFileSubmitter(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldValueEntity fieldValue,
        SubmissionFileData file,
        bool inBatch,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        dbContext.SubmissionHistoryEntries.Add(new SubmissionHistoryFileDownloadedEntity
        {
            Submission = submission,
            OccuredAt = _clock.GetCurrentInstant(),

            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = false,
            InstigatorUserId = userId,

            FieldId = fieldValue.FieldId,
            FileId = file.Id,
            InBatch = inBatch,
        });

        await dbContext.SaveChangesAsync(ct);
    }

    public async ValueTask DownloadFileStaff(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldValueEntity fieldValue,
        SubmissionFileData file,
        bool inBatch,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        dbContext.SubmissionHistoryEntries.Add(new SubmissionHistoryFileDownloadedEntity
        {
            Submission = submission,
            OccuredAt = _clock.GetCurrentInstant(),

            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = true,
            InstigatorUserId = userId,

            FieldId = fieldValue.FieldId,
            FileId = file.Id,
            InBatch = inBatch,
        });

        await dbContext.SaveChangesAsync(ct);
    }
}