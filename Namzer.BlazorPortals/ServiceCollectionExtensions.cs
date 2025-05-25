using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Namzer.BlazorPortals;

public static class ServiceCollectionExtensions
{
    public static void AddPortals(this IServiceCollection services) => services.TryAddScoped<PortalRegistration>();
}
