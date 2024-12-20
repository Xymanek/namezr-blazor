using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Namezr.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAppShared();

await builder.Build().RunAsync();