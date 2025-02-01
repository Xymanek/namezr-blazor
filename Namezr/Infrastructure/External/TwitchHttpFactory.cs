using Microsoft.Extensions.Options;
using TwitchLib.Api.Core.HttpCallHandlers;
using TwitchLib.Api.Core.Interfaces;

namespace Namezr.Infrastructure.External;

public interface ITwitchHttpFactory
{
    IHttpCallHandler Create();
}

[AutoConstructor]
[RegisterSingleton]
internal partial class TwitchHttpFactory : ITwitchHttpFactory
{
    private readonly ILogger<TwitchHttpClient> _twitchHttpClientLogger;
    private readonly IOptionsMonitor<TwitchApiOptions> _twitchApiOptions;

    public IHttpCallHandler Create()
    {
        IHttpCallHandler httpClient = new TwitchHttpClient(_twitchHttpClientLogger);
        TwitchApiOptions options = _twitchApiOptions.CurrentValue;

        if (options.MockServerUrl is not null)
        {
            httpClient = new MockApiHttpClientDecorator
            {
                Inner = httpClient,
                MockServerUrl = options.MockServerUrl,
            };
        }

        return httpClient;
    }
}