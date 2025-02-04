using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Namezr.Infrastructure.Auth;

internal interface ILoginProviderHandlerCollection : IReadOnlyDictionary<string, ILoginProviderHandler>
{
    bool TryGetLogMissing(
        string loginProvider,
        [MaybeNullWhen(false)] out ILoginProviderHandler handler,
        ILogger logger,
        [CallerMemberName] string? caller = null
    );

    ILoginProviderHandler? MaybeGetLogMissing(
        string loginProvider,
        ILogger logger,
        [CallerMemberName] string? caller = null
    );
}

[RegisterSingleton]
internal partial class LoginProviderHandlerCollection : ILoginProviderHandlerCollection
{
    private readonly IReadOnlyDictionary<string, ILoginProviderHandler> _loginProviderHandlers;

    public LoginProviderHandlerCollection(
        IEnumerable<ILoginProviderHandler> loginProviderHandlers
    )
    {
        _loginProviderHandlers = loginProviderHandlers
            .ToDictionary(x => x.LoginProvider);
    }

    public bool TryGetLogMissing(
        string loginProvider,
        [MaybeNullWhen(false)] out ILoginProviderHandler handler,
        ILogger logger,
        [CallerMemberName] string? caller = null
    )
    {
        bool result = _loginProviderHandlers.TryGetValue(loginProvider, out handler);
        
        if (!result)
        {
            LogLoginProviderHandlerMissing(logger, loginProvider, caller ?? "unknown");
        }

        return result;
    }
    
    public ILoginProviderHandler? MaybeGetLogMissing(
        string loginProvider,
        ILogger logger,
        [CallerMemberName] string? caller = null
    )
    {
        if (_loginProviderHandlers.TryGetValue(loginProvider, out ILoginProviderHandler? handler))
        {
            return handler;
        }
        
        LogLoginProviderHandlerMissing(logger, loginProvider, caller ?? "unknown");
        return null;
    }

    [LoggerMessage(
        LogLevel.Warning,
        "Missing handler for login provider ({loginProvider}) (attempt to use by {caller})"
    )]
    private static partial void LogLoginProviderHandlerMissing(ILogger logger, string loginProvider, string caller);

    #region IReadOnlyDictionary implementation proxy

    public IEnumerator<KeyValuePair<string, ILoginProviderHandler>> GetEnumerator()
    {
        return _loginProviderHandlers.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_loginProviderHandlers).GetEnumerator();
    }

    public int Count => _loginProviderHandlers.Count;

    public bool ContainsKey(string key)
    {
        return _loginProviderHandlers.ContainsKey(key);
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out ILoginProviderHandler value)
    {
        return _loginProviderHandlers.TryGetValue(key, out value);
    }

    public ILoginProviderHandler this[string key] => _loginProviderHandlers[key];

    public IEnumerable<string> Keys => _loginProviderHandlers.Keys;

    public IEnumerable<ILoginProviderHandler> Values => _loginProviderHandlers.Values;

    #endregion
}