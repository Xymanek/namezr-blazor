using Namezr.Features.Identity.Data;

namespace Namezr.Infrastructure.Auth;

public interface ILoginProviderHandler
{
    /// <summary>
    /// The authentication scheme name of the external login provider.
    /// </summary>
    string LoginProvider { get; }

    /// <summary>
    /// Called when a user signs in with an external login provider using an existing association.
    /// </summary>
    ValueTask OnSignIn(ExternalSignInInfo signInInfo);

    /// <summary>
    /// Called when an external login is being associated with a user.
    /// Allows to augment <paramref name="userLogin"/> before it is saved to the database.
    /// </summary>
    ValueTask OnAssociate(ApplicationUserLogin userLogin, ExternalSignInInfo signInInfo);
}