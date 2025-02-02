namespace Namezr.Infrastructure.Auth;

public interface ILoginProviderHandler
{
    string LoginProvider { get; }

    ValueTask OnSignInAsync(ExternalSignInInfo signInInfo);
}