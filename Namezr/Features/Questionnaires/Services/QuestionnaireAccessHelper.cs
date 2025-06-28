using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Namezr.Features.Identity.Data;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Eligibility.Services;
using Namezr.Infrastructure.Data;
using Namezr.Client.Public.Questionnaires;
using Namezr.Features.Questionnaires.Services;
using Namezr.Client;
using NodaTime;
using Namezr.Features.Questionnaires.Pages;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Consumers.Services;

namespace Namezr.Features.Questionnaires.Services;

public class QuestionnaireAccessResult
{
    public QuestionnaireVersionEntity? VersionEntity { get; set; }
    public QuestionnaireConfigModel? ConfigModel { get; set; }
    public Guid? QuestionnaireVersionId { get; set; }
    public EligibilityResult? EligibilityResult { get; set; }
    public QuestionnaireSubmissionEntity? ExistingSubmission { get; set; }
    public Dictionary<string, SubmissionValueModel>? InitialValues { get; set; }
    public DisabledReason? DisabledReason { get; set; }
}

public enum DisabledReason
{
    SubmissionsClosed,
    NotLoggedIn,
    NotEligible,
    AlreadyApproved,
}

public class QuestionnaireAccessHelper
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEligibilityService _eligibilityService;
    private readonly IdentityUserAccessor _userAccessor;
    private readonly IFieldValueSerializer _fieldValueSerializer;
    private readonly IClock _clock;

    public QuestionnaireAccessHelper(
        ApplicationDbContext dbContext,
        IEligibilityService eligibilityService,
        IdentityUserAccessor userAccessor,
        IFieldValueSerializer fieldValueSerializer,
        IClock clock)
    {
        _dbContext = dbContext;
        _eligibilityService = eligibilityService;
        _userAccessor = userAccessor;
        _fieldValueSerializer = fieldValueSerializer;
        _clock = clock;
    }

    public async Task<QuestionnaireAccessResult> GetAccessInfoAsync(
        Guid questionnaireId,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var result = new QuestionnaireAccessResult();

        var versionEntity = await _dbContext.QuestionnaireVersions
            .AsSplitQuery()
            .AsNoTracking()
            .Include(x => x.Questionnaire.EligibilityConfiguration).ThenInclude(x => x.Options)
            .Include(x => x.Questionnaire.Creator.SupportTargets)
            .Include(x => x.Fields!).ThenInclude(x => x.Field)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(q => q.Questionnaire.Id == questionnaireId, cancellationToken);

        result.VersionEntity = versionEntity;
        if (versionEntity is null)
        {
            result.DisabledReason = null;
            return result;
        }

        if (versionEntity.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.Closed)
        {
            result.DisabledReason = DisabledReason.SubmissionsClosed;
        }

        result.ConfigModel = SubmissionMapper.MapToConfigModel(versionEntity);
        result.QuestionnaireVersionId = versionEntity.Id;

        ApplicationUser? user = await _userAccessor.GetUserAsync(httpContext);
        if (user is null)
        {
            result.DisabledReason ??= DisabledReason.NotLoggedIn;
            return result;
        }

        result.EligibilityResult = await _eligibilityService.ClassifyEligibility(
            user.Id,
            versionEntity.Questionnaire.EligibilityConfiguration,
            UserStatusSyncEagerness.Default
        );

        if (!result.EligibilityResult.Any)
        {
            result.DisabledReason ??= DisabledReason.NotEligible;
        }

        var existingSubmission = await _dbContext.QuestionnaireSubmissions
            .AsSplitQuery()
            .Include(x => x.FieldValues)
            .Include(x => x.History!.Where(entry => entry is SubmissionHistoryPublicCommentEntity))
            .Include(x => x.Labels!.Where(label => label.IsSubmitterVisible))
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.Version.QuestionnaireId == questionnaireId, cancellationToken);

        result.ExistingSubmission = existingSubmission;

        if (existingSubmission is null)
        {
            if (versionEntity.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.EditExistingOnly)
            {
                result.DisabledReason ??= DisabledReason.SubmissionsClosed;
            }
        }
        else
        {
            if (
                existingSubmission.ApprovedAt is not null &&
                versionEntity.Questionnaire.ApprovalMode == QuestionnaireApprovalMode.RequireApprovalProhibitEditingApproved
            )
            {
                result.DisabledReason ??= DisabledReason.AlreadyApproved;
            }

            result.InitialValues = new Dictionary<string, SubmissionValueModel>();
            foreach (var fieldValue in existingSubmission.FieldValues!)
            {
                var fieldType = versionEntity.Fields!
                    .SingleOrDefault(fieldConfig => fieldConfig.FieldId == fieldValue.FieldId)
                    ?.Field.Type;
                if (fieldType is null) continue;
                result.InitialValues.Add(
                    fieldValue.FieldId.ToString(),
                    _fieldValueSerializer.Deserialize(fieldType.Value, fieldValue.ValueSerialized)
                );
            }
        }

        return result;
    }
}
