using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using FluentValidation.Results;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Medallion.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Files;
using Namezr.Features.Files.Services;
using Namezr.Features.Identity.Data;
using Namezr.Features.Notifications.Contracts;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Notifications;
using Namezr.Features.Questionnaires.Pages;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.QuestionnaireSubmissionSave)]
internal partial class SubmissionSaveEndpoint
{
    // TODO: refactor, this has seriously overgrown
    private static async ValueTask<Guid> HandleAsync(
        SubmissionCreateModel model,
        IHttpContextAccessor httpContextAccessor,
        IdentityUserAccessor userAccessor,
        IClock clock,
        IDistributedLockProvider distributedLockProvider,
        IFieldValueSerializer fieldValueSerializer,
        ApplicationDbContext dbContext,
        IFileUploadTicketHelper fileTicketHelper,
        ISubmissionAuditService auditService,
        INotificationDispatcher notificationDispatcher,
        CancellationToken ct
    )
    {
        QuestionnaireVersionEntity? questionnaireVersion = await dbContext.QuestionnaireVersions
            .AsNoTracking()
            .Include(x => x.Questionnaire)
            .Include(x => x.Fields!).ThenInclude(x => x.Field)
            .SingleOrDefaultAsync(x => x.Id == model.QuestionnaireVersionId, ct);

        if (questionnaireVersion is null)
        {
            // TODO: return 400
            throw new Exception("Questionnaire version not found");
        }

        if (questionnaireVersion.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.Closed)
        {
            // TODO: return 400
            throw new Exception("Questionnaire is closed for submissions");
        }

        // TODO: map only the field configs
        QuestionnaireConfigModel configModel = questionnaireVersion.MapToConfigModel();
        SubmissionModelValuesValidator valuesOnlyValidator = new(
            new SubmissionValuesValidator<Guid>(configModel, guid => guid)
        );

        ValidationResult valuesValidationResult = await valuesOnlyValidator.ValidateAsync(model, ct);
        if (!valuesValidationResult.IsValid)
        {
            ThrowFailedValuesValidation();
        }

        // TODO: validate files

        Dictionary<Guid, UploadedFileInfo> uploadedFileInfos;
        try
        {
            uploadedFileInfos = model.NewFileTickets
                .Select(fileTicketHelper.UnprotectUploadedForCurrentUser)
                .ToDictionary(x => x.FileId, x => x);
        }
        catch (TicketUnprotectionFailedException)
        {
            valuesValidationResult.Errors.Add(new ValidationFailure(
                nameof(SubmissionCreateModel.NewFileTickets),
                "Invalid ticket(s)"
            ));
            ThrowFailedValuesValidation();
        }

        // TODO: validate eligibility + generally replicate the same checks as
        // Namezr.Features.Questionnaires.Pages.QuestionnaireHome.DisabledReason

        Dictionary<Guid, QuestionnaireFieldConfigurationEntity> fieldConfigsById
            = questionnaireVersion.Fields!.ToDictionary(x => x.Field.Id, x => x);

        HttpContext httpContext = httpContextAccessor.HttpContext!;
        ApplicationUser currentUser = await userAccessor.GetRequiredUserAsync(httpContext);

        // Ensure 1 submission per ques per user, even in race conditions.
        await using var _ = await distributedLockProvider.AcquireLockAsync(
            GetLockName(questionnaireVersion.QuestionnaireId, currentUser.Id),
            cancellationToken: ct
        );

        QuestionnaireSubmissionEntity? submissionEntity = await dbContext.QuestionnaireSubmissions
            .AsTracking()
            .Include(x => x.FieldValues)
            .FirstOrDefaultAsync(
                s =>
                    s.UserId == currentUser.Id &&
                    s.Version.QuestionnaireId == questionnaireVersion.QuestionnaireId,
                cancellationToken: ct
            );

        if (
            submissionEntity is null &&
            questionnaireVersion.Questionnaire.SubmissionOpenMode == QuestionnaireSubmissionOpenMode.EditExistingOnly
        )
        {
            // TODO: return 400
            throw new Exception("Questionnaire is closed for submissions");
        }

        if (
            submissionEntity is { ApprovedAt: not null } &&
            questionnaireVersion.Questionnaire.ApprovalMode ==
            QuestionnaireApprovalMode.RequireApprovalProhibitEditingApproved
        )
        {
            valuesValidationResult.Errors.Add(new ValidationFailure(
                nameof(SubmissionCreateModel.QuestionnaireVersionId),
                "Editing approved submissions is not allowed"
            ));
            ThrowFailedValuesValidation();
        }

        HashSet<Guid> fileUploadFieldIds = questionnaireVersion.Fields!
            .Where(x => x.Field.Type == QuestionnaireFieldType.FileUpload)
            .Select(x => x.Field.Id)
            .ToHashSet();

        Dictionary<Guid, Dictionary<Guid, SubmissionFileData>> existingFiles;
        if (submissionEntity is not null)
        {
            existingFiles = submissionEntity.FieldValues!
                .Where(x => fileUploadFieldIds.Contains(x.FieldId))
                .ToDictionary(
                    valueEntity => valueEntity.FieldId,
                    valueEntity => fieldValueSerializer
                        .Deserialize(QuestionnaireFieldType.FileUpload, valueEntity.ValueSerialized)
                        .FileValue!
                        .ToDictionary(x => x.Id, x => x)
                );
        }
        else
        {
            existingFiles = [];
        }

        foreach (Guid fileUploadFieldId in fileUploadFieldIds)
        {
            List<SubmissionFileData> submittedFiles
                = model.Values.GetValueOrDefault(fileUploadFieldId)?.FileValue ?? [];

            Dictionary<Guid, SubmissionFileData> existingFieldFiles
                = existingFiles.GetValueOrDefault(fileUploadFieldId) ?? [];

            foreach (SubmissionFileData submittedFile in submittedFiles)
            {
                if (existingFieldFiles.TryGetValue(submittedFile.Id, out SubmissionFileData? existingFile))
                {
                    if (existingFile != submittedFile)
                    {
                        valuesValidationResult.Errors.Add(new ValidationFailure(
                            nameof(SubmissionCreateModel.Values),
                            "Existing file was changed"
                        ));
                        ThrowFailedValuesValidation();
                    }
                }
                else
                {
                    if (!uploadedFileInfos.TryGetValue(submittedFile.Id, out UploadedFileInfo? uploadedFileInfo))
                    {
                        valuesValidationResult.Errors.Add(new ValidationFailure(
                            nameof(SubmissionCreateModel.Values),
                            "New file is missing ticket"
                        ));
                        ThrowFailedValuesValidation();
                    }

                    if (!uploadedFileInfo.Matches(submittedFile))
                    {
                        valuesValidationResult.Errors.Add(new ValidationFailure(
                            nameof(SubmissionCreateModel.Values),
                            "File does not match ticket"
                        ));
                        ThrowFailedValuesValidation();
                    }
                }
            }
        }

        if (submissionEntity != null)
        {
            submissionEntity.VersionId = model.QuestionnaireVersionId;
            submissionEntity.SubmittedAt = clock.GetCurrentInstant();

            if (
                questionnaireVersion.Questionnaire.ApprovalMode ==
                QuestionnaireApprovalMode.RequireApprovalRemoveWhenEdited
            )
            {
                submissionEntity.ApprovedAt = null;
                submissionEntity.ApproverId = null;
            }

            dbContext.SubmissionHistoryEntries.Add(auditService.UpdateValues(submissionEntity));

            // TODO: fire the notification only if there were ever some changes(/views?) by staff
            dbContext.OnSavedChangesOnce((_, _) =>
            {
                notificationDispatcher.Dispatch(new SubmitterUpdatedValuesNotificationData
                {
                    CreatorId = questionnaireVersion.Questionnaire.CreatorId,
                    QuestionnaireId = questionnaireVersion.Questionnaire.Id,
                    SubmitterId = currentUser.Id,
                    SubmissionId = submissionEntity.Id,
                    SubmissionUrl = UriHelper.BuildAbsolute(
                        httpContext.Request.Scheme,
                        httpContext.Request.Host,
                        httpContext.Request.PathBase,
                        // TODO: update once we support multiple submissions per questionnaire
                        $"/questionnaires/{questionnaireVersion.Questionnaire.Id}"
                    ),
                });
            });
        }
        else
        {
            submissionEntity = new()
            {
                VersionId = model.QuestionnaireVersionId,
                UserId = currentUser.Id,

                SubmittedAt = clock.GetCurrentInstant(),
            };

            dbContext.QuestionnaireSubmissions.Add(submissionEntity);
            dbContext.SubmissionHistoryEntries.Add(auditService.InitialSubmit(submissionEntity));
        }

        // Even if we loaded an existing entity, outright replace old value entities/rows
        submissionEntity.FieldValues = model.Values
            .Select(pair => new QuestionnaireFieldValueEntity
            {
                FieldId = pair.Key,
                ValueSerialized = fieldValueSerializer.Serialize(fieldConfigsById[pair.Key].Field.Type, pair.Value),
            })
            .ToHashSet();

        if (questionnaireVersion.Questionnaire.ApprovalMode == QuestionnaireApprovalMode.GrantAutomatically)
        {
            submissionEntity.ApprovedAt = clock.GetCurrentInstant();
            submissionEntity.ApproverId = null;
        }

        await dbContext.SaveChangesAsync(ct);

        return submissionEntity.Id;

        [DoesNotReturn]
        void ThrowFailedValuesValidation()
        {
            // TODO: field ID is not included in the error message,
            // e.g. `Values.NumberValue` instead of `Values[Guid].NumberValue`
            throw new ValidationException(valuesValidationResult.Errors);
        }
    }

    private class SubmissionModelValuesValidator : AbstractValidator<SubmissionCreateModel>
    {
        public SubmissionModelValuesValidator(IValidator<Dictionary<Guid, SubmissionValueModel>> fieldsValidator)
        {
            RuleFor(x => x.Values)
                .SetValidator(fieldsValidator);
        }
    }

    private static string GetLockName(Guid questionnaireId, Guid userId)
    {
        return $"namezr_questionnaire-submission-save_{questionnaireId}_{userId}";
    }
}