using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Namezr.Client;

public static class ClientSentryBootstrap
{
    public const string FunctionName = "AppGlobal_GetSentryDsn";

    internal static void SetupSentry(this WebAssemblyHostBuilder builder)
    {
        string? dsn;
        try
        {
            dsn = builder.GetJsRuntime().Invoke<string?>(FunctionName);
        }
        catch (Exception e)
        {
            Console.WriteLine("Error while retrieving Sentry DSN: " + e);
            return;
        }

        if (dsn is null)
        {
            Console.WriteLine("No Sentry DSN found");
            return;
        }

        builder.UseSentry(options =>
        {
            options.Dsn = dsn;
            options.TracesSampleRate = 0.1;
        });
    }
}