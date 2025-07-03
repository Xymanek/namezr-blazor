using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using FluentValidation.Results;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Medallion.Threading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Namezr.Client;
using Namezr.Client.Public.Questionnaires;
using Namezr.Client.Studio.Questionnaires.Edit;
using Namezr.Features.Files;
using Namezr.Features.Files.Services;
using Namezr.Features.Notifications.Contracts;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Notifications;
using Namezr.Features.Questionnaires.Services;
using Namezr.Infrastructure.Data;
using Namezr.Features.Questionnaires.Pages;
using NodaTime;
using Namezr.Features.Questionnaires.Models;
using Namezr.Features.Identity.Data;
using Namezr.Features.Identity.Helpers;

namespace Namezr.Features.Questionnaires.Endpoints;

[Handler]
[Authorize]
[MapPost(ApiEndpointPaths.QuestionnaireSubmissionSave)]
internal partial class SubmissionSaveEndpoint
{
    // TODO: refactor, this has seriously overgrown
    private static async ValueTask<Guid> HandleAsync(
        SubmissionCreateModel model,
        IClock clock,
        IDistributedLockProvider distributedLockProvider,
        IFieldValueSerializer fieldValueSerializer,
        ApplicationDbContext dbContext,
        IFileUploadTicketHelper fileTicketHelper,
        ISubmissionAuditService auditService,
        INotificationDispatcher notificationDispatcher,
        IQuestionnaireSubmissionContextService contextService,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        CancellationToken ct
    )
    {
        QuestionnaireVersionEntity? questionnaireVersion = await contextService.GetQuestionnaireVersionAsync(model.QuestionnaireVersionId, ct);
        if (questionnaireVersion is null)
        {
            // TODO: return 400
            throw new Exception($"Questionnaire with ID {model.QuestionnaireVersionId} not found.");
        }

        // Fetch the current user for the request
        ApplicationUser? currentUser = await userAccessor.GetUserAsync(httpContextAccessor.HttpContext!);

        // Ensure 1 submission per ques per user, even in race conditions.
        await using var _ = await distributedLockProvider.AcquireLockAsync(
            GetLockName(questionnaireVersion.QuestionnaireId, currentUser!.Id),
            cancellationToken: ct
        );

        // Determine submission mode based on whether SubmissionId is provided
        SubmissionMode submissionMode = model.SubmissionId.HasValue 
            ? SubmissionMode.EditExisting 
            : SubmissionMode.CreateNew;
            
        QuestionnaireSubmissionContext context = await contextService.GetSubmissionContextAsync(questionnaireVersion, currentUser, submissionMode, ct);

        if (context.DisabledReason.HasValue)
        {
            // TODO
            throw new Exception($"Submission saving is not allowed: {context.DisabledReason.Value}");
        }

        // Select the submission to edit if SubmissionId is provided, otherwise null (for new)
        QuestionnaireSubmissionEntity? submissionEntity = null;
        if (model.SubmissionId.HasValue)
        {
            submissionEntity = context.ExistingSubmissions
                .FirstOrDefault(s => s.Id == model.SubmissionId.Value);

            if (submissionEntity == null)
            {
                // TODO: return 400
                throw new Exception("Submission not found or not owned by user.");
            }
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

        Dictionary<Guid, QuestionnaireFieldConfigurationEntity> fieldConfigsById
            = questionnaireVersion.Fields!.ToDictionary(x => x.Field.Id, x => x);

        var httpContext = httpContextAccessor.HttpContext!;

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
                    CreatorDisplayName = questionnaireVersion.Questionnaire.Creator.DisplayName,
                    QuestionnaireId = questionnaireVersion.Questionnaire.Id,
                    QuestionnaireName = questionnaireVersion.Questionnaire.Title,
                    SubmitterId = currentUser.Id,
                    SubmitterName = currentUser.UserName!,
                    SubmissionId = submissionEntity.Id,
                    SubmissionNumber = submissionEntity.Number,
                    SubmissionStudioUrl = UriHelper.BuildAbsolute(
                        httpContext.Request.Scheme,
                        httpContext.Request.Host,
                        httpContext.Request.PathBase,
                        $"/studio/{questionnaireVersion.Questionnaire.CreatorId.NoHyphens()}/questionnaires/{questionnaireVersion.Questionnaire.Id.NoHyphens()}/submissions/{submissionEntity.Id.NoHyphens()}"
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