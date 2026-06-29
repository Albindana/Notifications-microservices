using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationTemplate> Templates => Set<NotificationTemplate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.UserId).IsRequired();
            entity.Property(n => n.RecipientEmail).IsRequired();
            entity.Property(n => n.Subject).IsRequired();
            entity.Property(n => n.Type).HasConversion<string>();
            entity.Property(n => n.Channel).HasConversion<string>();
            entity.Property(n => n.Status).HasConversion<string>();
            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => n.Status);
        });

        builder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Type).HasConversion<string>();
            entity.Property(t => t.Subject).IsRequired();
            entity.Property(t => t.BodyTemplate).IsRequired();
            entity.HasIndex(t => t.Type).IsUnique();
        });
    }
}
