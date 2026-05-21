using FlowBoard.Infrastructure;
using FlowBoard.Infrastructure.Data;
using FlowBoard.Web.Components;
using FlowBoard.Web.Hubs;
using FlowBoard.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSignalR();
builder.Services.AddSingleton<BoardUpdateNotifier>();
builder.Services.AddSingleton<FlowBoard.Application.Interfaces.IPresenceService, FlowBoard.Infrastructure.Services.PresenceService>();
builder.Services.AddScoped<AppActionDispatcher>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddFlowBoardInfrastructure(
    builder.Configuration,
    builder.Environment.IsDevelopment());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "flowboard",
    environment = app.Environment.EnvironmentName,
    timestampUtc = DateTimeOffset.UtcNow
}));

app.MapGet("/ready", () => Results.Ok(new
{
    status = "ready",
    service = "flowboard",
    timestampUtc = DateTimeOffset.UtcNow
}));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHub<BoardHub>("/hubs/board")
    .DisableAntiforgery();

app.MapAdditionalIdentityEndpoints();

var initializeDatabaseOnStartup = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("FlowBoard:InitializeDatabaseOnStartup");
var applySchemaChangesOnStartup = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("FlowBoard:ApplySchemaChangesOnStartup");
var seedDemoDataOnStartup = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("FlowBoard:SeedDemoDataOnStartup");

if (initializeDatabaseOnStartup)
{
    await SeedData.InitializeAsync(
        app.Services,
        applySchemaChangesOnStartup,
        seedDemoDataOnStartup);
}

app.Run();
