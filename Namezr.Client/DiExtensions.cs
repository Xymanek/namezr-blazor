using BlazorColorPicker;
using Havit.Blazor.Components.Web;
using Namzer.BlazorPortals;

namespace Namezr.Client;

public static class DiExtensions
{
    /// <summary>
    /// Service registration for both WASM app and server-side app.
    /// </summary>
    public static void AddAppShared(this IServiceCollection services)
    {
        services.AddHxServices();
        services.AddHxMessenger();

        services.AddPortals();
        services.AddColorPicker();

        services.AutoRegister();
    }
}