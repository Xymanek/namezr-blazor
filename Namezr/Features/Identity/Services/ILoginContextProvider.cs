using CommunityToolkit.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;

namespace Namezr.Features.Identity.Services;

internal interface ILoginContextProvider
{
    string Provider { get; }

    // TODO: pass application login with loaded third party token
    Task<LoginContext> GetLoginContextAsync(string providerKey, CancellationToken ct = default);
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

    public async Task<LoginContext> GetLoginContextAsync(string providerKey, CancellationToken ct = default)
    {
        LoginContext? value = await _cache.GetOrCreateAsync<LoginContext>(
            $"LoginContextProvider__{Provider}__{providerKey}",
            _ => FetchLoginContextAsync(providerKey, ct),
            new MemoryCacheEntryOptions
            {
                // TODO: make configurable
                SlidingExpiration = TimeSpan.FromMinutes(20),
            }
        );

        Guard.IsNotNull(value);
        return value;
    }

    protected abstract Task<LoginContext> FetchLoginContextAsync(string providerKey, CancellationToken ct);
}