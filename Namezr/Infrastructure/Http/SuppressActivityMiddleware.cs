using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace Namezr.Infrastructure.Http;

/// <summary>
/// Prevents trace (<see cref="T:System.Diagnostics.Activity"/>) export for certain requests.
/// </summary>
internal class SuppressActivityMiddleware(RequestDelegate next)
{
    public static readonly object EndpointMetadataItem = new();

    public Task InvokeAsync(HttpContext context)
    {
        // Inspired by https://martinjt.me/2022/07/29/removing-static-file-traces-from-asp-net-core/
        // However, StaticFileOptions is no longer hooked up to the pipeline in the same way
        // so we process the endpoint metadata instead.
        if (Activity.Current != null && ShouldSuppressed(context))
        {
            Activity.Current.IsAllDataRequested = false;
        }
        
        return next(context);
    }

    private static bool ShouldSuppressed(HttpContext context)
    {
        // Hardcode exception for blazor JS setup - it's very annoying to attach endpoint metadata to them
        if (context.Request.Path.StartsWithSegments("/_framework"))
        {
            return true;
        }
        
        if (
            context.Features.Get<IEndpointFeature>() is { Endpoint: {} endpoint } &&
            endpoint.Metadata.Contains(EndpointMetadataItem)
        )
        {
            return true;
        }
        
        return false;
    }
}