using AspireRunner.AspNetCore;
using AspNet.Security.OAuth.Twitch;
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
using Namezr.Infrastructure.Twitch;
using Namezr.Infrastructure.Twitch.MockServer;
using NodaTime;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using SystemClock = NodaTime.SystemClock;

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

builder.Services
    .AddOptions<TwitchOptions>()
    .BindConfiguration(TwitchOptions.SectionPath)
    .ValidateDataAnnotations();

TwitchOptions twitchOptions = builder.Configuration
    .GetRequiredSection(TwitchOptions.SectionPath)
    .Get<TwitchOptions>()!;

if (twitchOptions.MockServerUrl is not null && builder.Environment.IsProduction())
{
    Console.Error.WriteLine("Twitch mock server is not supported in production, ignoring");
}

if (twitchOptions.MockServerUrl is not null && !builder.Environment.IsProduction())
{
    // TODO: the whole mock server integration is an extremely shoddy implementation

    builder.Services.AddAuthentication()
        .AddOAuth<TwitchAuthenticationOptions, MockServerAuthenticationHandler>(
            TwitchAuthenticationDefaults.AuthenticationScheme,
            TwitchAuthenticationDefaults.DisplayName,
            options =>
            {
                options.ClientId = twitchOptions.OAuth.ClientId;
                options.ClientSecret = twitchOptions.OAuth.ClientSecret;

                options.SaveTokens = true;

                options.Scope.Add("user:read:subscriptions");
                options.Scope.Add("user:read:follows");

                options.Scope.Add("channel:read:subscriptions");
                options.Scope.Add("moderator:read:followers");
            }
        );
}
else
{
    builder.Services.AddAuthentication().AddTwitch(options =>
    {
        options.ClientId = twitchOptions.OAuth.ClientId;
        options.ClientSecret = twitchOptions.OAuth.ClientSecret;

        options.SaveTokens = true;

        options.Scope.Add("user:read:subscriptions");
        options.Scope.Add("user:read:follows");

        options.Scope.Add("channel:read:subscriptions");
        options.Scope.Add("moderator:read:followers");
    });
}

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
        .WithTracing(tracing =>
        {
            tracing.AddSource(Diagnostics.ActivitySourceName);
        })
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