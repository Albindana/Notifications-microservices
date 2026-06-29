using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UserService.Domain.Entities;

namespace UserService.Infrastructure.Persistence;

public static class DbSeeder
{
    /// <summary>
    /// Applies migrations and seeds the default admin/test accounts on startup.
    /// </summary>
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<AppUser>>();
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("UserService.DbSeeder");

        await db.Database.MigrateAsync();

        await SeedUserAsync(userManager, logger,
            email: "admin@notify.com", password: "Admin123!",
            firstName: "Admin", lastName: "User");

        await SeedUserAsync(userManager, logger,
            email: "user@notify.com", password: "User123!",
            firstName: "Test", lastName: "User");
    }

    private static async Task SeedUserAsync(
        UserManager<AppUser> userManager, ILogger logger,
        string email, string password, string firstName, string lastName)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            logger.LogInformation("Seeded user {Email}", email);
        else
            logger.LogWarning("Failed to seed user {Email}: {Errors}",
                email, string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
