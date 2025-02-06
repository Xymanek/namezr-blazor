using AspireRunner.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Namezr;
using Namezr.Client;
using Namezr.Components;
using Namezr.Components.Account;
using Namezr.Features.Identity.Data;
using Namezr.Infrastructure.Auth;
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

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();
builder.Services.AddAuthorization();

builder.Services.AddAuthentication().AddTwitch(options =>
{
    options.ClientId =
        builder.Configuration["Twitch:ClientId"] ?? throw new Exception("Missing Twitch:ClientId");

    options.ClientSecret =
        builder.Configuration["Twitch:ClientSecret"] ?? throw new Exception("Missing Twitch:ClientSecret");

    options.SaveTokens = true;
    
    options.Scope.Add("user:read:subscriptions");
    options.Scope.Add("user:read:follows");
});

builder.Services.AddAuthentication().AddPatreon(options =>
{
    options.ClientId =
        builder.Configuration["Patreon:ClientId"] ?? throw new Exception("Missing Patreon:ClientId");

    options.ClientSecret =
        builder.Configuration["Patreon:ClientSecret"] ?? throw new Exception("Missing Patreon:ClientSecret");

    options.SaveTokens = true;
});

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        // TODO: update this once the email management is finalized
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<SignInManager<ApplicationUser>, ApplicationSignInManager>();
builder.Services.AddScoped<IUserStore<ApplicationUser>, ApplicationUserStore>();

builder.Services.AddSingleton<IClock>(SystemClock.Instance);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddProblemDetails();

builder.AddNpgsqlDbContext<ApplicationDbContext>(
    connectionName: "postgresdb",
    configureDbContextOptions: ApplicationDbContext.DefaultConfigure
);

// AddNpgsqlDbContext uses AddDbContextPool which does not register an IDbContextFactory
// and everything's internal, so the only way to get a DbContext is to create a service scope...
// While this ctor is [EntityFrameworkInternal], it seems stable enough and all pooling setup is done already anyway.
// TODO: create GH issues.
#pragma warning disable EF1001
builder.Services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(
    sp => new PooledDbContextFactory<ApplicationDbContext>(
        sp.GetRequiredService<IDbContextPool<ApplicationDbContext>>()
    )
);
#pragma warning restore EF1001

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

app.MapAdditionalIdentityEndpoints();

app.Run();