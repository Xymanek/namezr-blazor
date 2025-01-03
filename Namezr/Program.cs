using Microsoft.EntityFrameworkCore;
using Namezr;
using Namezr.Client;
using Namezr.Components;
using Namezr.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppShared();
builder.Services.AutoRegister();

builder.Services.AddNamezrHandlers();
builder.Services.AddNamezrBehaviors();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddProblemDetails();

builder.Services.AddDbContextFactory<ApplicationDbContext>(opt =>
{
    opt.UseNpgsql(
        builder.Configuration.GetConnectionString("Default"),
        postges =>
        {
            postges.UseNodaTime();
        }
    );
});

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

app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapNamezrEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Namezr.Client._Imports).Assembly);

app.Run();