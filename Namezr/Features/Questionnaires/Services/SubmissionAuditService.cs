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
    SubmissionHistoryInitialSubmitEntity CreateInitialSubmit(QuestionnaireSubmissionEntity submission);

    ValueTask InitialSubmit(QuestionnaireSubmissionEntity submission, CancellationToken ct);

    [MustUseReturnValue]
    SubmissionHistoryUpdatedValuesEntity CreateUpdateValues(QuestionnaireSubmissionEntity submission);

    ValueTask UpdateValues(QuestionnaireSubmissionEntity submission, CancellationToken ct);

    [MustUseReturnValue]
    SubmissionHistoryApprovalGrantedEntity CreateApprovalGrant(QuestionnaireSubmissionEntity submission);

    ValueTask GrantApproval(QuestionnaireSubmissionEntity submission, CancellationToken ct);

    [MustUseReturnValue]
    SubmissionHistoryApprovalRemovedEntity CreateApprovalRemoval(QuestionnaireSubmissionEntity submission);

    ValueTask RemoveApproval(QuestionnaireSubmissionEntity submission, CancellationToken ct);

    [MustUseReturnValue]
    SubmissionHistoryInternalNoteEntity CreateInternalNote(QuestionnaireSubmissionEntity submission, string content);

    ValueTask AddInternalNote(QuestionnaireSubmissionEntity submission, string content, CancellationToken ct);

    [MustUseReturnValue]
    SubmissionHistoryPublicCommentEntity CreatePublicComment(QuestionnaireSubmissionEntity submission, string content,
        bool isStaff);

    ValueTask AddPublicComment(QuestionnaireSubmissionEntity submission, string content, bool isStaff,
        CancellationToken ct);

    [MustUseReturnValue]
    SubmissionHistoryStaffViewedEntity CreateStaffView(QuestionnaireSubmissionEntity submission);

    ValueTask RecordStaffView(QuestionnaireSubmissionEntity submission, CancellationToken ct);
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

    public SubmissionHistoryInitialSubmitEntity CreateInitialSubmit(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        return new SubmissionHistoryInitialSubmitEntity
        {
            Submission = submission,
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
        
        var entry = CreateInitialSubmit(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryUpdatedValuesEntity CreateUpdateValues(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        return new SubmissionHistoryUpdatedValuesEntity
        {
            Submission = submission,
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
        
        var entry = CreateUpdateValues(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryApprovalGrantedEntity CreateApprovalGrant(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        return new SubmissionHistoryApprovalGrantedEntity
        {
            Submission = submission,
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
        
        var entry = CreateApprovalGrant(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryApprovalRemovedEntity CreateApprovalRemoval(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        return new SubmissionHistoryApprovalRemovedEntity
        {
            Submission = submission,
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
        
        var entry = CreateApprovalRemoval(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryInternalNoteEntity CreateInternalNote(
        QuestionnaireSubmissionEntity submission,
        string content)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        return new SubmissionHistoryInternalNoteEntity
        {
            Submission = submission,
            OccuredAt = _clock.GetCurrentInstant(),
            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = true,
            InstigatorUserId = userId,
            Content = content
        };
    }

    public async ValueTask AddInternalNote(
        QuestionnaireSubmissionEntity submission,
        string content,
        CancellationToken ct)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var entry = CreateInternalNote(submission, content);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryPublicCommentEntity CreatePublicComment(
        QuestionnaireSubmissionEntity submission,
        string content,
        bool isStaff)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        return new SubmissionHistoryPublicCommentEntity
        {
            Submission = submission,
            OccuredAt = _clock.GetCurrentInstant(),
            InstigatorIsProgrammatic = false,
            InstigatorIsStaff = isStaff,
            InstigatorUserId = userId,
            Content = content
        };
    }

    public async ValueTask AddPublicComment(
        QuestionnaireSubmissionEntity submission,
        string content,
        bool isStaff,
        CancellationToken ct)
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        
        var entry = CreatePublicComment(submission, content, isStaff);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }

    public SubmissionHistoryStaffViewedEntity CreateStaffView(
        QuestionnaireSubmissionEntity submission)
    {
        Guid userId = _userAccessor.GetRequiredUserId(_httpContextAccessor.HttpContext!);

        return new SubmissionHistoryStaffViewedEntity
        {
            Submission = submission,
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
        
        var entry = CreateStaffView(submission);
        dbContext.SubmissionHistoryEntries.Add(entry);
        await dbContext.SaveChangesAsync(ct);
    }
}