using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Data;

namespace Namezr.Infrastructure.Auth;

internal class ApplicationUserStore : UserOnlyStore<
    ApplicationUser,
    ApplicationDbContext,
    Guid,
    IdentityUserClaim<Guid>,
    ApplicationUserLogin,
    IdentityUserToken<Guid>
>
{
    private readonly ILoginProviderHandlerCollection _loginProviderHandlers;
    private readonly ILogger<ApplicationUserStore> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor - looks ugly
    public ApplicationUserStore(
        ApplicationDbContext context,
        ILoginProviderHandlerCollection loginProviderHandlers, 
        ILogger<ApplicationUserStore> logger, 
        IdentityErrorDescriber? describer = null
    ) : base(context, describer)
    {
        _loginProviderHandlers = loginProviderHandlers;
        _logger = logger;
    }

    public override async Task AddLoginAsync(
        ApplicationUser user,
        UserLoginInfo login,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(login);
        
        if (login is not ExternalLoginInfo externalLoginInfo)
        {
            throw new ArgumentException($"Only {nameof(ExternalLoginInfo)} are supported.");
        }

        ApplicationUserLogin userLogin = CreateUserLogin(user, login);
        
        if (_loginProviderHandlers.TryGetLogMissing(
                externalLoginInfo.LoginProvider, out ILoginProviderHandler? handler, _logger
            ))
        {
            await handler.OnAssociate(userLogin, new ExternalSignInInfo
            {
                User = user,
                ExternalLoginInfo = externalLoginInfo,
            });
        }
        
        UserLogins.Add(userLogin);
    }
}