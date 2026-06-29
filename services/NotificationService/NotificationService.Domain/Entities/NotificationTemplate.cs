using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Entities;

public class NotificationTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = default!;
    public string BodyTemplate { get; set; } = default!;  // supports {{FirstName}}, {{ResetLink}} placeholders
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
