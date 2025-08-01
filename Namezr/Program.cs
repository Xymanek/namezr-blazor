using AspireRunner.AspNetCore;
using AspNet.Security.OAuth.Discord;
using AspNet.Security.OAuth.Twitch;
using MailKitSimplified.Sender;
using Medallion.Threading;
using Medallion.Threading.Postgres;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Namezr;
using Namezr.Client;
using Namezr.Components;
using Namezr.Features.Identity.Helpers;
using Namezr.Features.Consumers.Services;
using Namezr.Features.Files.Configuration;
using Namezr.Features.Identity.Data;
using Namezr.Features.Identity.Endpoints;
using Namezr.Features.Notifications.Services;
using Namezr.Features.ThirdParty.Cli;
using Namezr.Infrastructure.Auth;
using Namezr.Infrastructure.Data;
using Namezr.Infrastructure.Discord;
using Namezr.Infrastructure.Http;
using Namezr.Infrastructure.OAuth;
using Namezr.Infrastructure.Twitch;
using Namezr.Infrastructure.Twitch.MockServer;
using NodaTime;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using SystemClock = NodaTime.SystemClock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddHttpClientInstrumentation();

        tracing.AddSource(Diagnostics.ActivitySourceName);
    });

string? sentryDsn = builder.Configuration["Sentry:Dsn"];
if (sentryDsn is not null)
{
    builder.WebHost.UseSentry(sentry =>
    {
        sentry.Dsn = sentryDsn;

        // Enabled traces
        sentry.TracesSampleRate = 1.0;

        sentry.UseOpenTelemetry();

        // SSR Blazor redirects
        sentry.AddExceptionFilterForType<NavigationException>();
    });

    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing =>
        {
            tracing.AddSentry();
        });
}

builder.AddServiceDefaults();

builder.Services.AddAppShared();
builder.Services.AutoRegister();

builder.Services.AddHostedService<PeriodicConsumerStatusSyncer>();
builder.Services.AddHostedService<ThreadPoolNotificationDispatcher>();

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

    options.Scope.Add("identity[email]");
    options.Scope.Add("identity.memberships");
    options.Scope.Add("campaigns");
    options.Scope.Add("w:campaigns.webhook");
    options.Scope.Add("campaigns.members");

    // Get the email for the initial sign up
    options.Fields.Add("email");
});

builder.Services.AddAuthentication().AddGoogle(options =>
{
    options.ClientId =
        builder.Configuration["Google:ClientId"] ?? throw new Exception("Missing Google:ClientId");

    options.ClientSecret =
        builder.Configuration["Google:ClientSecret"] ?? throw new Exception("Missing Google:ClientSecret");

    options.SaveTokens = true;
    options.AccessType = "offline";
});

builder.Services.AddSingleton(typeof(IOAuthTokenRefresher<>), typeof(OAuthTokenRefresher<>));

// AddOAuth() call since we replace the auth handler
builder.Services.AddAuthentication()
    .AddOAuth<DiscordAuthenticationOptions, NamezrDiscordAuthenticationHandler>(
        DiscordAuthenticationDefaults.AuthenticationScheme,
        DiscordAuthenticationDefaults.DisplayName,
        options =>
        {
            options.ClientId =
                builder.Configuration["Discord:ClientId"] ?? throw new Exception("Missing Discord:ClientId");

            options.ClientSecret =
                builder.Configuration["Discord:ClientSecret"] ?? throw new Exception("Missing Discord:ClientSecret");

            options.SaveTokens = true;

            options.Scope.Add("email");
            options.Scope.Add("guilds");
            options.Scope.Add("guilds.members.read");
        }
    );

builder.Services.AddOptions<DiscordAppOptions>()
    .BindConfiguration(DiscordAppOptions.SectionPath);

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

builder.Services.AddSingleton<IDistributedLockProvider>(_ => new PostgresDistributedSynchronizationProvider(
    builder.Configuration.GetConnectionString("postgresdb")!
));

builder.Services.AddOptions<FilesOptions>()
    .BindConfiguration(FilesOptions.SectionPath);

builder.Services.AddMemoryCache();

builder.Services.AddMailKitSimplifiedEmailSender(builder.Configuration);

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

if (args.FirstOrDefault() == "migrate-db")
{
    await using ApplicationDbContext dbContext = app.Services
        .GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
        .CreateDbContext();

    dbContext.Database.Migrate();
    return;
}

if (args.FirstOrDefault() == "print-third-party-token")
{
    await app.Services
        .GetRequiredService<PrintThirdPartyTokenCommand>()
        .Execute(args.Skip(1));

    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseMiddleware<SuppressActivityMiddleware>();

app.MapStaticAssets()
    .Add(endpoint => endpoint.Metadata.Add(SuppressActivityMiddleware.EndpointMetadataItem));

app.MapObservabilityEndpoints();

app.UseAuthorization();

app.UseAntiforgery();

app.MapNamezrEndpoints();
app.MapAdditionalIdentityEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Namezr.Client._Imports).Assembly);

// Validate startup is an additional step to building the C# solution to ensure that the "static configuration" is valid:
// * All application host initialization logic is executed successfully
// * DI container passes validation
// * Entity Framework Core model is built and validated
// And exists (instead of running forever) for ease of use in CLI workflows, such as CI/CD pipelines.
if (args.FirstOrDefault() == "validate-startup")
{
    await using ApplicationDbContext dbContext = app.Services
        .GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
        .CreateDbContext();

    // Force the model to be built and validated
    _ = dbContext.Model;
    
    Console.WriteLine("Startup validation completed successfully.");
    return;
}

app.Run();