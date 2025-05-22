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

    [MustUseReturnValue]
    SubmissionHistoryInitialSubmitEntity InitialSubmit(QuestionnaireSubmissionEntity submission);

    [MustUseReturnValue]
    SubmissionHistoryUpdatedValuesEntity UpdateValues(QuestionnaireSubmissionEntity submission);

    [MustUseReturnValue]
    SubmissionHistoryApprovalRemovedEntity ApprovalRemoval(QuestionnaireSubmissionEntity submission);

    [MustUseReturnValue]
    SubmissionHistoryApprovalGrantedEntity ApprovalGrant(QuestionnaireSubmissionEntity submission);

    [MustUseReturnValue]
    SubmissionHistoryStaffViewedEntity StaffView(QuestionnaireSubmissionEntity submission);

    SubmissionHistoryFileDownloadedEntity DownloadFileStaffPrepare(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldValueEntity fieldValue,
        SubmissionFileData file,
        bool inBatch
    );
}

[AutoConstructor]
[RegisterScoped]
internal partial class SubmissionAuditService : ISubmissionAuditService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SubmissionAuditService> _logger;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly IClock _clock;

    public SubmissionHistoryLabelAppliedEntity LabelAddedStaff(
        QuestionnaireSubmissionEntity submission,
        SubmissionLabelEntity label
    )
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        _logger.LogInformation(
            "Staff member {UserId} applied label {LabelId} '{LabelName}' to submission {SubmissionId}",
            userId, label.Id, label.Text, submission.Id);

        return new SubmissionHistoryLabelAppliedEntity
        {
            SubmissionId = submission.Id,
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

        _logger.LogInformation(
            "Staff member {UserId} removed label {LabelId} '{LabelName}' from submission {SubmissionId}",
            userId, label.Id, label.Text, submission.Id);

        return new SubmissionHistoryLabelRemovedEntity
        {
            SubmissionId = submission.Id,
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

        _logger.LogInformation(
            "Submitter {UserId} downloaded file {FileId} '{FileName}' from field {FieldId} in submission {SubmissionId}, in batch: {InBatch}",
            userId, file.Id, file.Name, fieldValue.FieldId, submission.Id, inBatch);

        dbContext.SubmissionHistoryEntries.Add(new SubmissionHistoryFileDownloadedEntity
        {
            SubmissionId = submission.Id,
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

        dbContext.SubmissionHistoryEntries.Add(DownloadFileStaffPrepare(submission, fieldValue, file, inBatch));

        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryFileDownloadedEntity DownloadFileStaffPrepare(
        QuestionnaireSubmissionEntity submission,
        QuestionnaireFieldValueEntity fieldValue,
        SubmissionFileData file,
        bool inBatch
    )
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        _logger.LogInformation(
            "Staff member {UserId} downloaded file {FileId} '{FileName}' from field {FieldId} in submission {SubmissionId}, in batch: {InBatch}",
            userId, file.Id, file.Name, fieldValue.FieldId, submission.Id, inBatch);

        SubmissionHistoryFileDownloadedEntity entry = new()
        {
            SubmissionId = submission.Id,
            OccuredAt = _clock.GetCurrentInstant(),

            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = true,
            InstigatorUserId = userId,

            FieldId = fieldValue.FieldId,
            FileId = file.Id,
            InBatch = inBatch,
        };
        
        return entry;
    }

    public SubmissionHistoryInitialSubmitEntity InitialSubmit(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        _logger.LogInformation(
            "User {UserId} made initial submission {SubmissionId} for questionnaire version {VersionId}",
            userId, submission.Id, submission.VersionId);

        return new SubmissionHistoryInitialSubmitEntity
        {
            SubmissionId = submission.Id,
            OccuredAt = _clock.GetCurrentInstant(),
            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = false,
            InstigatorUserId = userId
        };
    }

    public async ValueTask InitialSubmit(
        QuestionnaireSubmissionEntity submission,
        CancellationToken ct)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var entry = InitialSubmit(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryUpdatedValuesEntity UpdateValues(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        _logger.LogInformation("User {UserId} updated values for submission {SubmissionId}",
            userId, submission.Id);

        return new SubmissionHistoryUpdatedValuesEntity
        {
            SubmissionId = submission.Id,
            OccuredAt = _clock.GetCurrentInstant(),
            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = false,
            InstigatorUserId = userId
        };
    }

    public async ValueTask UpdateValues(
        QuestionnaireSubmissionEntity submission,
        CancellationToken ct)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var entry = UpdateValues(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryApprovalRemovedEntity ApprovalRemoval(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        _logger.LogInformation("Staff member {UserId} removed approval for submission {SubmissionId}",
            userId, submission.Id);

        return new SubmissionHistoryApprovalRemovedEntity
        {
            SubmissionId = submission.Id,
            OccuredAt = _clock.GetCurrentInstant(),
            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = true,
            InstigatorUserId = userId
        };
    }

    public async ValueTask RemoveApproval(
        QuestionnaireSubmissionEntity submission,
        CancellationToken ct)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var entry = ApprovalRemoval(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryApprovalGrantedEntity ApprovalGrant(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        _logger.LogInformation("Staff member {UserId} granted approval for submission {SubmissionId}",
            userId, submission.Id);

        return new SubmissionHistoryApprovalGrantedEntity
        {
            SubmissionId = submission.Id,
            OccuredAt = _clock.GetCurrentInstant(),
            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = true,
            InstigatorUserId = userId
        };
    }

    public async ValueTask GrantApproval(
        QuestionnaireSubmissionEntity submission,
        CancellationToken ct)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var entry = ApprovalGrant(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }


    public SubmissionHistoryStaffViewedEntity StaffView(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        _logger.LogInformation(
            "Staff member {UserId} viewed submission {SubmissionId}",
            userId, submission.Id);

        return new SubmissionHistoryStaffViewedEntity
        {
            SubmissionId = submission.Id,
            OccuredAt = _clock.GetCurrentInstant(),
            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = true,
            InstigatorUserId = userId
        };
    }

    public async ValueTask RecordStaffView(
        QuestionnaireSubmissionEntity submission,
        CancellationToken ct)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);

        var entry = StaffView(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }
}