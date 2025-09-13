using System.Diagnostics;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Namezr.Client;
using Namezr.Client.Shared;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Notifications.Contracts;
using Namezr.Features.Questionnaires.Data;
using Namezr.Features.Questionnaires.Notifications;
using Namezr.Infrastructure.Data;
using NodaTime;

namespace Namezr.Features.Questionnaires.Endpoints;

public record SubmitSubmissionCommentRequest
{
    public required Guid SubmissionId { get; init; }
    public required string Content { get; init; }
    public required StudioSubmissionCommentType Type { get; init; }
}

public record SubmitSubmissionCommentResponse
{
    public required bool Success { get; init; }
}

[Handler]
[Behaviors] // Clear out the validation
[MapPost(ApiEndpointPaths.SubmissionCommentSubmit)]
internal partial class SubmitSubmissionCommentEndpoint
{
    private static async ValueTask<SubmitSubmissionCommentResponse> HandleAsync(
        SubmitSubmissionCommentRequest request,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IdentityUserAccessor userAccessor,
        IHttpContextAccessor httpContextAccessor,
        IClock clock,
        INotificationDispatcher notificationDispatcher,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            throw new Exception("Comment content cannot be empty");
        }

        if (request.Content.Length > SubmissionHistoryEntryEntity.CommentContentMaxLength)
        {
            throw new Exception($"Comment content cannot exceed {SubmissionHistoryEntryEntity.CommentContentMaxLength} characters");
        }

        HttpContext httpContext = httpContextAccessor.HttpContext!;
        await using ApplicationDbContext dbContext = await dbContextFactory.CreateDbContextAsync(ct);

        QuestionnaireSubmissionEntity? submission = await dbContext.QuestionnaireSubmissions
            .Include(x => x.Version.Questionnaire.Creator)
            .SingleOrDefaultAsync(x => x.Id == request.SubmissionId, ct);

        if (submission == null)
        {
            throw new Exception("Bad submission ID");
        }

        await ValidateAccess();

        SubmissionHistoryEntryEntity historyEntry = request.Type switch
        {
            StudioSubmissionCommentType.InternalNote => new SubmissionHistoryInternalNoteEntity
            {
                Content = request.Content,
                OccuredAt = clock.GetCurrentInstant(),
                InstigatorIsStaff = true,
                InstigatorIsProgrammatic = false,
                InstigatorUserId = userAccessor.GetRequiredUserId(httpContext),
                SubmissionId = submission.Id,
            },
            StudioSubmissionCommentType.PublicComment => new SubmissionHistoryPublicCommentEntity
            {
                Content = request.Content,
                OccuredAt = clock.GetCurrentInstant(),
                InstigatorIsStaff = true,
                InstigatorIsProgrammatic = false,
                InstigatorUserId = userAccessor.GetRequiredUserId(httpContext),
                SubmissionId = submission.Id,
            },

            _ => throw new UnreachableException(),
        };

        dbContext.SubmissionHistoryEntries.Add(historyEntry);
        await dbContext.SaveChangesAsync(ct);

        // Dispatch notification for public staff comment
        if (request.Type == StudioSubmissionCommentType.PublicComment)
        {
            notificationDispatcher.Dispatch(new SubmissionStaffActionUserNotificationData
            {
                CreatorId = submission.Version.Questionnaire.CreatorId,
                CreatorDisplayName = submission.Version.Questionnaire.Creator.DisplayName,
                QuestionnaireId = submission.Version.QuestionnaireId,
                QuestionnaireName = submission.Version.Questionnaire.Title,
                SubmitterId = submission.UserId,
                SubmissionId = submission.Id,
                SubmissionNumber = submission.Number,

                SubmissionPublicUrl = UriHelper.BuildAbsolute(
                    httpContext.Request.Scheme,
                    httpContext.Request.Host,
                    httpContext.Request.PathBase,
                    $"/questionnaires/{submission.Version.QuestionnaireId.NoHyphens()}/{submission.Id.NoHyphens()}"
                ),
                Type = SubmissionStaffActionType.CommentAdded,

                CommentBody = request.Content
            });
        }

        return new SubmitSubmissionCommentResponse
        {
            Success = true
        };

        async Task ValidateAccess()
        {
            Guid userId = userAccessor.GetRequiredUserId(httpContext);

            // ReSharper disable once AccessToDisposedClosure
            bool isCreatorStaff = await dbContext.CreatorStaff
                .Where(staff =>
                    staff.UserId == userId &&
                    staff.CreatorId == submission.Version.Questionnaire.CreatorId
                )
                .AnyAsync(ct);

            if (isCreatorStaff) return;

            // TODO: correct
            throw new Exception("Access denied");
        }
    }
}