using Microsoft.Extensions.Logging;
using NotificationService.Application.Common;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;

namespace NotificationService.Application.Notifications;

public class NotificationDispatchService : INotificationService
{
    private readonly INotificationRepository _notifications;
    private readonly ITemplateRepository _templates;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<NotificationDispatchService> _logger;

    public NotificationDispatchService(
        INotificationRepository notifications,
        ITemplateRepository templates,
        IEmailSender emailSender,
        ILogger<NotificationDispatchService> logger)
    {
        _notifications = notifications;
        _templates = templates;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Notification> SendAsync(SendNotificationCommand command, CancellationToken ct = default)
    {
        var (subject, body) = await BuildContentAsync(command.Type, command.TemplateData, ct);

        var notification = new Notification
        {
            UserId = command.UserId,
            RecipientEmail = command.RecipientEmail,
            Type = command.Type,
            Channel = command.Channel,
            Subject = subject,
            Body = body,
            Status = NotificationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _notifications.AddAsync(notification, ct);
        await _notifications.SaveChangesAsync(ct);

        await DeliverAsync(notification, ct);
        return notification;
    }

    public async Task<Notification> RetryAsync(Guid notificationId, CancellationToken ct = default)
    {
        var notification = await _notifications.GetByIdAsync(notificationId, ct)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

        notification.RetryCount++;
        await DeliverAsync(notification, ct);
        return notification;
    }

    private async Task DeliverAsync(Notification notification, CancellationToken ct)
    {
        try
        {
            await _emailSender.SendAsync(
                notification.RecipientEmail, notification.Subject, notification.Body, ct);

            notification.MarkSent();
            _logger.LogInformation(
                "Notification {Id} ({Type}) sent to {Email}",
                notification.Id, notification.Type, notification.RecipientEmail);
        }
        catch (Exception ex)
        {
            notification.MarkFailed(ex.Message);
            _logger.LogError(ex,
                "Notification {Id} ({Type}) failed for {Email}",
                notification.Id, notification.Type, notification.RecipientEmail);
        }

        await _notifications.SaveChangesAsync(ct);
    }

    private async Task<(string Subject, string Body)> BuildContentAsync(
        NotificationType type, IReadOnlyDictionary<string, string> data, CancellationToken ct)
    {
        var template = await _templates.GetByTypeAsync(type, ct);

        if (template is not null)
        {
            return (
                TemplateRenderer.Render(template.Subject, data),
                TemplateRenderer.Render(template.BodyTemplate, data));
        }

        // Fallback so delivery still works if a template is missing.
        var fallbackBody = string.Join(Environment.NewLine,
            data.Select(kv => $"{kv.Key}: {kv.Value}"));
        return ($"Notification: {type}", fallbackBody);
    }
}
