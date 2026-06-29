using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Infrastructure.Persistence;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;
    public NotificationRepository(NotificationDbContext db) => _db = db;

    public async Task AddAsync(Notification notification, CancellationToken ct = default) =>
        await _db.Notifications.AddAsync(notification, ct);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<Notification>> ListAsync(CancellationToken ct = default) =>
        await _db.Notifications.OrderByDescending(n => n.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<Notification>> ListByUserAsync(string userId, CancellationToken ct = default) =>
        await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

public class TemplateRepository : ITemplateRepository
{
    private readonly NotificationDbContext _db;
    public TemplateRepository(NotificationDbContext db) => _db = db;

    public Task<NotificationTemplate?> GetByTypeAsync(NotificationType type, CancellationToken ct = default) =>
        _db.Templates.FirstOrDefaultAsync(t => t.Type == type, ct);

    public Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.Templates.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<NotificationTemplate>> ListAsync(CancellationToken ct = default) =>
        await _db.Templates.OrderBy(t => t.Type).ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
