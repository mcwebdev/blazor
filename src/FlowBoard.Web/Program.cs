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
app.MapHub<BoardHub>("/hubs/board");

app.MapAdditionalIdentityEndpoints();

// Initialize database and seed data (runs migrations on startup)
await SeedData.InitializeAsync(app.Services);

app.Run();
