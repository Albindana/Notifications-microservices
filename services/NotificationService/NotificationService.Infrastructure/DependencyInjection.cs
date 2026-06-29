using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Services;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? "Data Source=notificationservice.db";

        services.AddDbContext<NotificationDbContext>(options =>
        {
            if (IsSqlServer(connectionString))
                options.UseSqlServer(connectionString);
            else
                options.UseSqlite(connectionString);
        });

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<IEmailSender, ConsoleEmailSender>();

        return services;
    }

    private static bool IsSqlServer(string connectionString)
    {
        var lower = connectionString.ToLowerInvariant();
        return lower.Contains("server=") || lower.Contains("initial catalog=")
            || lower.Contains("database=");
    }
}
