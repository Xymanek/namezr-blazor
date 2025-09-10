using CommunityToolkit.Diagnostics;
using Immediate.Handlers.Shared;
using Microsoft.EntityFrameworkCore;
using Namezr.Client.Contracts.Auth;
using Namezr.Features.Identity.Helpers;
using Namezr.Infrastructure.Data;

namespace Namezr.Infrastructure.Auth;

[AutoConstructor]
internal partial class AuthorizationBehaviour<TRequest, TResponse> : Behavior<TRequest, TResponse>
    where TRequest : IAuthorizableRequest
{
    private readonly ILogger<AuthorizationBehaviour<TRequest, TResponse>> _logger;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IdentityUserAccessor _userAccessor;

    public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken ct)
    {
        HttpContext? httpContext = _httpContextAccessor.HttpContext;
        Guard.IsNotNull(httpContext);

        if (!_userAccessor.TryGetUserId(httpContext, out Guid userId))
        {
            LogUnauthorizedRequestNoUserId();
            ThrowHelper.ThrowInvalidOperationException("Unauthorized request: no user ID found in the HTTP context.");
        }

        if (request is ICreatorManagementRequest creatorManagementRequest)
        {
            await CheckCreatorManagementRequestAsync(creatorManagementRequest, userId, ct);
        }
        else if (request is IQuestionnaireManagementRequest questionnaireManagementRequest)
        {
            await CheckQuestionnaireManagementRequestAsync(questionnaireManagementRequest, userId, ct);
        }
        else if (request is ISeriesManagementRequest seriesManagementRequest)
        {
            await CheckSeriesManagementRequestAsync(seriesManagementRequest, userId, ct);
        }
        else if (request is ISubmissionManagementRequest submissionManagementRequest)
        {
            await CheckSubmissionManagementRequestAsync(submissionManagementRequest, userId, ct);
        }
        else if (request is IPollManagementRequest pollManagementRequest)
        {
            await CheckPollManagementRequestAsync(pollManagementRequest, userId, ct);
        }

        // If the request does not require specific authorization, proceed to the next behavior.
        return await Next(request, ct);
    }

    private async ValueTask CheckCreatorManagementRequestAsync(
        ICreatorManagementRequest request,
        Guid userId,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        Guid creatorId = request.CreatorId;

        bool isCreatorStaff = await dbContext.CreatorStaff
            .Where(staff => staff.UserId == userId && staff.CreatorId == creatorId)
            .AnyAsync(ct);

        if (isCreatorStaff) return;

        LogAuthorizationFailedNotCreatorStaff(userId, creatorId);
        ThrowAuthFailed("Current user is not staff of the creator.");
    }

    private async ValueTask CheckQuestionnaireManagementRequestAsync(
        IQuestionnaireManagementRequest request,
        Guid userId,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        Guid questionnaireId = request.QuestionnaireId;

        bool isCreatorStaff = await dbContext.CreatorStaff
            .Where(staff => staff.UserId == userId && staff.Creator.Questionnaires!.Any(q => q.Id == questionnaireId))
            .AnyAsync(ct);

        if (isCreatorStaff) return;

        LogAuthorizationFailedNotQuestionnaireStaff(userId, questionnaireId);
        ThrowAuthFailed("Current user is not staff of the creator that owns the questionnaire.");
    }

    private async ValueTask CheckSeriesManagementRequestAsync(
        ISeriesManagementRequest request,
        Guid userId,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        Guid seriesId = request.SeriesId;

        bool isCreatorStaff = await dbContext.CreatorStaff
            .Where(staff => staff.UserId == userId && staff.Creator.Questionnaires!
                .Any(q => q.SelectionSeries!.Any(s => s.Id == seriesId)))
            .AnyAsync(ct);

        if (isCreatorStaff) return;

        LogAuthorizationFailedNotSeriesStaff(userId, seriesId);
        ThrowAuthFailed("Current user is not staff of the creator that owns the selection series.");
    }

    private async ValueTask CheckSubmissionManagementRequestAsync(
        ISubmissionManagementRequest request,
        Guid userId,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        Guid submissionId = request.SubmissionId;

        bool isCreatorStaff = await dbContext.CreatorStaff
            .Where(staff => staff.UserId == userId && staff.Creator.Questionnaires!
                .Any(q => q.Versions!.Any(v => v.Submissions!.Any(s => s.Id == submissionId))))
            .AnyAsync(ct);

        if (isCreatorStaff) return;

        LogAuthorizationFailedNotSubmissionStaff(userId, submissionId);
        ThrowAuthFailed("Current user is not staff of the creator that owns the submission.");
    }

    private async ValueTask CheckPollManagementRequestAsync(
        IPollManagementRequest request,
        Guid userId,
        CancellationToken ct
    )
    {
        await using ApplicationDbContext dbContext = await _dbContextFactory.CreateDbContextAsync(ct);
        Guid pollId = request.PollId;

        bool isCreatorStaff = await dbContext.CreatorStaff
            .Where(staff => staff.UserId == userId && staff.Creator.Polls!.Any(p => p.Id == pollId))
            .AnyAsync(ct);

        if (isCreatorStaff) return;

        LogAuthorizationFailedNotPollStaff(userId, pollId);
        ThrowAuthFailed("Current user is not staff of the creator that owns the poll.");
    }

    private static void ThrowAuthFailed(string message)
    {
        throw new AuthorizationFailedException(message);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Unauthorized request: no user ID found in the HTTP context."
    )]
    private partial void LogUnauthorizedRequestNoUserId();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Authorization failed: User {UserId} is not staff of creator {CreatorId}."
    )]
    private partial void LogAuthorizationFailedNotCreatorStaff(Guid userId, Guid creatorId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Authorization failed: User {UserId} is not staff of the creator that owns questionnaire {QuestionnaireId}."
    )]
    private partial void LogAuthorizationFailedNotQuestionnaireStaff(Guid userId, Guid questionnaireId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Authorization failed: User {UserId} is not staff of the creator that owns selection series {SeriesId}."
    )]
    private partial void LogAuthorizationFailedNotSeriesStaff(Guid userId, Guid seriesId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Authorization failed: User {UserId} is not staff of the creator that owns submission {SubmissionId}."
    )]
    private partial void LogAuthorizationFailedNotSubmissionStaff(Guid userId, Guid submissionId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Authorization failed: User {UserId} is not staff of the creator that owns poll {PollId}."
    )]
    private partial void LogAuthorizationFailedNotPollStaff(Guid userId, Guid pollId);
}
