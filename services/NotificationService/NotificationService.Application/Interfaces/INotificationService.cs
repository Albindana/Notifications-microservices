using NotificationService.Application.Notifications;
using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

public interface INotificationService
{
    /// <summary>
    /// Renders the template, persists the notification, sends it via the email stub,
    /// and records the outcome (Sent/Failed). Returns the persisted notification.
    /// </summary>
    Task<Notification> SendAsync(SendNotificationCommand command, CancellationToken ct = default);

    /// <summary>
    /// Re-attempts delivery of a previously failed notification.
    /// </summary>
    Task<Notification> RetryAsync(Guid notificationId, CancellationToken ct = default);
}
