using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Namezr.Features.Identity.Data;

namespace Namezr.Infrastructure.Auth;

public partial class ApplicationSignInManager : SignInManager<ApplicationUser>
{
    private readonly ILogger<ApplicationSignInManager> _logger;
    private readonly IReadOnlyDictionary<string, ILoginProviderHandler> _loginProviderHandlers;

    public ApplicationSignInManager(
        UserManager<ApplicationUser> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<ApplicationUser>> baseLogger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<ApplicationUser> confirmation,
        ILogger<ApplicationSignInManager> logger,
        IEnumerable<ILoginProviderHandler> loginProviderHandlers
    ) : base(
        userManager, contextAccessor, claimsFactory,
        optionsAccessor, baseLogger, schemes, confirmation
    )
    {
        _logger = logger;

        _loginProviderHandlers = loginProviderHandlers
            .ToDictionary(x => x.LoginProvider);
    }

    // TODO: SignInWithClaimsAsync (for 1st time)

    public override async Task SignInWithClaimsAsync(
        ApplicationUser user,
        AuthenticationProperties? authenticationProperties,
        IEnumerable<Claim> additionalClaims
    )
    {
        ExternalLoginInfo? externalLoginInfo = await GetExternalLoginInfoAsync();
        if (externalLoginInfo == null)
        {
            await InvokeBase();
            return;
        }

        await InvokeBase();
        await NotifyOnExternalSignIn(user, externalLoginInfo);

        return;

        Task InvokeBase()
        {
            return base.SignInWithClaimsAsync(user, authenticationProperties, additionalClaims);
        }
    }

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
            await NotifyOnExternalSignIn(user, externalLoginInfo);
        }

        return result;

        Task<SignInResult> InvokeBase()
        {
            return base.SignInOrTwoFactorAsync(user, isPersistent, loginProvider, bypassTwoFactor);
        }
    }

    private async Task NotifyOnExternalSignIn(ApplicationUser user, ExternalLoginInfo externalLoginInfo)
    {
        if (!_loginProviderHandlers.TryGetValue(externalLoginInfo.LoginProvider, out ILoginProviderHandler? handler))
        {
            LogLoginProviderHandlerMissing(externalLoginInfo.LoginProvider);
        }
        else
        {
            await handler.OnSignInAsync(new ExternalSignInInfo
            {
                User = user,
                LoginProvider = externalLoginInfo.LoginProvider,
                ExternalLoginInfo = externalLoginInfo,
            });
        }
    }

    [LoggerMessage(
        LogLevel.Warning,
        "Login provider mismatch between external login provider from info ({retrievedLoginProvider}) and " +
        "login provider currently being used to sign in ({expectedLoginProvider}). " +
        "Will not fire per-provider sign in events."
    )]
    private partial void LogLoginProviderMismatch(string retrievedLoginProvider, string expectedLoginProvider);

    [LoggerMessage(
        LogLevel.Warning,
        "Missing handler for login provider ({loginProvider}). " +
        "Will not fire per-provider sign in events."
    )]
    private partial void LogLoginProviderHandlerMissing(string loginProvider);
}