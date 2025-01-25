using AspireRunner.AspNetCore;
using Namezr;
using Namezr.Client;
using Namezr.Components;
using Namezr.Infrastructure.Data;
using NodaTime;
using OpenTelemetry;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddAppShared();
builder.Services.AutoRegister();

builder.Services.AddNamezrHandlers();
builder.Services.AddNamezrBehaviors();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IClock>(SystemClock.Instance);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddProblemDetails();

builder.AddNpgsqlDbContext<ApplicationDbContext>(
    connectionName: "postgresdb",
    configureDbContextOptions: ApplicationDbContext.DefaultConfigure
);

if (builder.Environment.IsDevelopment())
{
    // bind from configuration (appsettings.json, etc)
    builder.Services.AddAspireDashboard(config =>
    {
        builder.Configuration.GetSection("AspireDashboard").Bind(config);
    });

    builder.Services.AddOpenTelemetry()
        // TODO: figure out a way to unhardcode this (or switch to full aspire?)
        .UseOtlpExporter(OtlpExportProtocol.Grpc, new Uri("http://localhost:4317"));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.MapObservabilityEndpoints();

app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapNamezrEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Namezr.Client._Imports).Assembly);

app.Run();