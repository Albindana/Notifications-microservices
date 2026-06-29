using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> ListByUserAsync(string userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface ITemplateRepository
{
    Task<NotificationTemplate?> GetByTypeAsync(NotificationType type, CancellationToken ct = default);
    Task<NotificationTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationTemplate>> ListAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
