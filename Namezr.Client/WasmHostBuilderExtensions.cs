using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

namespace Namezr.Client;

internal static class WasmHostBuilderExtensions
{
    public static IJSInProcessRuntime GetJsRuntime(this WebAssemblyHostBuilder builder)
    {
        // HAX, relies on implementation details of WebAssemblyHostBuilder.InitializeDefaultServices

        return (IJSInProcessRuntime)builder.Services
            .Single(descriptor =>
                descriptor.Lifetime == ServiceLifetime.Singleton &&
                descriptor.ServiceType == typeof(IJSRuntime)
            )
            .ImplementationInstance!;
    }
}