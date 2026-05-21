using FlowBoard.Application.Interfaces;
using FlowBoard.Infrastructure.Data;
using FlowBoard.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowBoard.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFlowBoardInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        // ── Database ───────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        if (isDevelopment)
        {
            services.AddDbContext<FlowBoardDbContext>(options =>
                options.UseSqlite(connectionString));
        }
        else
        {
            services.AddDbContext<FlowBoardDbContext>(options =>
                options.UseNpgsql(connectionString));
        }

        // ── Identity ───────────────────────────────────────────────
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.SignIn.RequireConfirmedAccount = false;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<FlowBoardDbContext>()
        .AddDefaultTokenProviders();

        // ── Application Services ───────────────────────────────────
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IAuditQueryService, AuditQueryService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
