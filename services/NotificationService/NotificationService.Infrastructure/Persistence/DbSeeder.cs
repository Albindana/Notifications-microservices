using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        await db.Database.MigrateAsync();

        if (await db.Templates.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        db.Templates.AddRange(
            new NotificationTemplate
            {
                Type = NotificationType.Welcome,
                Subject = "Welcome to Notification Platform, {{FirstName}}!",
                BodyTemplate = "Hi {{FirstName}},\n\nThanks for registering on {{RegisteredAt}}. We're glad to have you!",
                CreatedAt = now,
                UpdatedAt = now
            },
            new NotificationTemplate
            {
                Type = NotificationType.PasswordReset,
                Subject = "Reset your password",
                BodyTemplate = "We received a request to reset your password.\n\nReset it here: {{ResetLink}}\n\nThis link expires at {{ExpiresAt}}.",
                CreatedAt = now,
                UpdatedAt = now
            },
            new NotificationTemplate
            {
                Type = NotificationType.ProfileUpdate,
                Subject = "Your profile was updated",
                BodyTemplate = "Hi {{FirstName}} {{LastName}},\n\nYour profile details were updated successfully.",
                CreatedAt = now,
                UpdatedAt = now
            });

        await db.SaveChangesAsync();
    }
}
