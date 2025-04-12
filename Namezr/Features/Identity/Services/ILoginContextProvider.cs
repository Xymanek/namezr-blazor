using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Namezr.Features.Identity.Data;

namespace Namezr.Features.Identity.Services;

internal interface ILoginContextProvider
{
    string Provider { get; }

    /// <param name="userLogin">
    /// Must Have <see cref="ApplicationUserLogin.ThirdPartyToken"/> loaded
    /// </param>
    /// <param name="ct"></param>
    Task<LoginContext> GetLoginContextAsync(ApplicationUserLogin userLogin, CancellationToken ct = default);
}

internal record LoginContext
{
    public required string DisplayName { get; init; }
    public string? ImageUrl { get; init; }
}

[AutoConstructor]
internal abstract partial class CachingLoginContextProviderBase : ILoginContextProvider
{
    private readonly IMemoryCache _cache;

    public abstract string Provider { get; }

    public async Task<LoginContext> GetLoginContextAsync(
        ApplicationUserLogin userLogin,
        CancellationToken ct = default
    )
    {
        LoginContext? value = await _cache.GetOrCreateAsync<LoginContext>(
            $"LoginContextProvider__{Provider}__{userLogin.ProviderKey}",
            _ => FetchLoginContextAsync(userLogin, ct),
            new MemoryCacheEntryOptions
            {
                // TODO: make configurable
                SlidingExpiration = TimeSpan.FromMinutes(20),
            }
        );

        Guard.IsNotNull(value);
        return value;
    }

    protected abstract Task<LoginContext> FetchLoginContextAsync(ApplicationUserLogin userLogin, CancellationToken ct);
}