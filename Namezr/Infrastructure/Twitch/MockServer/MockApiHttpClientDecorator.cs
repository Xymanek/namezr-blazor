using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Interfaces;

namespace Namezr.Infrastructure.Twitch.MockServer;

public class MockApiHttpClientDecorator : IHttpCallHandler
{
    public required IHttpCallHandler Inner { private get; init; }
    public required string MockServerUrl { private get; init; }

    // TODO: figure out the correct values
    #nullable disable
    
    public Task<KeyValuePair<int, string>> GeneralRequestAsync(
        string url, string method, string payload = null,
        ApiVersion api = ApiVersion.Helix,
        string clientId = null, string accessToken = null
    )
    {
        return Inner.GeneralRequestAsync(RewriteUrl(url), method, payload, api, clientId, accessToken);
    }

    public Task PutBytesAsync(string url, byte[] payload)
    {
        return Inner.PutBytesAsync(RewriteUrl(url), payload);
    }

    public Task<int> RequestReturnResponseCodeAsync(
        string url, string method,
        List<KeyValuePair<string, string>> getParams = null
    )
    {
        return Inner.RequestReturnResponseCodeAsync(RewriteUrl(url), method, getParams);
    }
    
    #nullable restore

    private string RewriteUrl(string url)
    {
        url = url.Replace("https://id.twitch.tv/oauth2", MockServerUrl + "/auth");
        url = url.Replace("https://api.twitch.tv/helix", MockServerUrl + "/mock");

        return url;
    }
}