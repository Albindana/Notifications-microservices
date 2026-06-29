using NotificationService.Domain.Enums;

namespace NotificationService.Application.Notifications;

/// <summary>
/// Parameter object describing a notification to dispatch. Built by event consumers
/// and passed to <see cref="Interfaces.INotificationService"/>.
/// </summary>
public class SendNotificationCommand
{
    public string UserId { get; set; } = default!;
    public string RecipientEmail { get; set; } = default!;
    public NotificationType Type { get; set; }
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;
    public Dictionary<string, string> TemplateData { get; set; } = new();
}
