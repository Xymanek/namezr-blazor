using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Namezr.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAppShared();

builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();