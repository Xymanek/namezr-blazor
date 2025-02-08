using Microsoft.Extensions.Options;
using Namezr.Infrastructure.Twitch.MockServer;
using TwitchLib.Api.Core.HttpCallHandlers;
using TwitchLib.Api.Core.Interfaces;

namespace Namezr.Infrastructure.Twitch;

public interface ITwitchHttpFactory
{
    IHttpCallHandler Create();
}

[AutoConstructor]
[RegisterSingleton]
internal partial class TwitchHttpFactory : ITwitchHttpFactory
{
    private readonly ILogger<TwitchHttpClient> _twitchHttpClientLogger;
    private readonly IOptionsMonitor<TwitchOptions> _twitchOptions;

    public IHttpCallHandler Create()
    {
        IHttpCallHandler httpClient = new TwitchHttpClient(_twitchHttpClientLogger);
        TwitchOptions options = _twitchOptions.CurrentValue;

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