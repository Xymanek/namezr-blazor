using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Namezr.Features.Identity.Data;

namespace Namezr.Infrastructure.Auth;

internal partial class ApplicationSignInManager : SignInManager<ApplicationUser>
{
    private readonly ILoginProviderHandlerCollection _loginProviderHandlers;
    private readonly ILogger<ApplicationSignInManager> _logger;

    public ApplicationSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> baseLogger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation,
        ILogger<ApplicationSignInManager> logger,
        ILoginProviderHandlerCollection loginProviderHandlers
    ) : base(
        userManager, contextAccessor, claimsFactory,
        optionsAccessor, baseLogger, schemes, confirmation
    )
    {
        _logger = logger;
        _loginProviderHandlers = loginProviderHandlers;
    }


    // If I was to override ExternalLoginSignInAsync, I'd need to fully replace the logic to be able to get
    // the user object. This way, I only decorate existing logic, not replace it.
    protected override async Task<SignInResult> SignInOrTwoFactorAsync(
        ApplicationUser user,
        bool isPersistent,
        string? loginProvider = null,
        bool bypassTwoFactor = false
    )
    {
        if (loginProvider == null)
        {
            return await InvokeBase();
        }

        // Need to get it here because base method does
        // await Context.SignOutAsync(IdentityConstants.ExternalScheme);
        ExternalLoginInfo? externalLoginInfo = await GetExternalLoginInfoAsync();
        if (externalLoginInfo == null) return await InvokeBase();

        if (loginProvider != externalLoginInfo.LoginProvider)
        {
            LogLoginProviderMismatch(externalLoginInfo.LoginProvider, loginProvider);
            return await InvokeBase();
        }

        SignInResult result = await InvokeBase();

        if (result.Succeeded)
        {
            if (_loginProviderHandlers.TryGetLogMissing(
                    externalLoginInfo.LoginProvider, out ILoginProviderHandler? handler, _logger
                ))
            {
                await handler.OnSignIn(new ExternalSignInInfo
                {
                    User = user,
                    ExternalLoginInfo = externalLoginInfo,
                });
            }
        }

        return result;

        Task<SignInResult> InvokeBase()
        {
            return base.SignInOrTwoFactorAsync(user, isPersistent, loginProvider, bypassTwoFactor);
        }
    }

    [LoggerMessage(
        LogLevel.Warning,
        "Login provider mismatch between external login provider from info ({retrievedLoginProvider}) and " +
        "login provider currently being used to sign in ({expectedLoginProvider}). " +
        "Will not fire per-provider sign in events."
    )]
    private partial void LogLoginProviderMismatch(string retrievedLoginProvider, string expectedLoginProvider);
}