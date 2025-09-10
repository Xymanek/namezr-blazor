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
}
