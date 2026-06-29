using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = default!;
    public string RecipientEmail { get; set; } = default!;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Subject { get; set; } = default!;
    public string Body { get; set; } = default!;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkFailed(string error)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = error;
    }
}
