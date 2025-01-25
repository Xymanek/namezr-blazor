using Namezr.Client.Infra;

namespace Namezr.Infrastructure.SpaStubs;

[RegisterSingleton]
public class ThrowingSpaHttpClientProvider : ISpaHttpClientProvider
{
    public HttpClient HttpClient => throw new Exception("Should never be called on the server");
}