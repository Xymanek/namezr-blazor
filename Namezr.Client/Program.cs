using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Namezr.Client;
using Namezr.Client.Infra;
using vNext.BlazorComponents.FluentValidation;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAppShared();

builder.Services.AddSingleton(new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<ISpaHttpClientProvider, SpaHttpClientProvider>();

builder.Services.AddSingleton<IValidatorFactory>(new DefaultValidatorFactory { DisableAssemblyScanning = true });

await builder.Build().RunAsync();