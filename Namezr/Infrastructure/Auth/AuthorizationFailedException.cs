namespace Namezr.Infrastructure.Auth;

/// <summary>
/// Thrown when the current user is not authorized to perform the requested action.
/// </summary>
public class AuthorizationFailedException : Exception
{
    public AuthorizationFailedException()
    {
    }

    public AuthorizationFailedException(string? message) : base(message)
    {
    }

    public AuthorizationFailedException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
