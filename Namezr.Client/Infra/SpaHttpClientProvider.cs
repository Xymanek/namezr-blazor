namespace Namezr.Client.Infra;

public interface ISpaHttpClientProvider
{
    HttpClient HttpClient { get; }
}

[AutoConstructor]
internal partial class SpaHttpClientProvider : ISpaHttpClientProvider
{
    public HttpClient HttpClient { get; }
}