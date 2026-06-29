using MassTransit;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Enums;
using Shared.Contracts.Events;

namespace NotificationService.Application.Consumers;

public class PasswordResetRequestedConsumer : IConsumer<PasswordResetRequestedEvent>
{
    private readonly INotificationService _notificationService;

    public PasswordResetRequestedConsumer(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<PasswordResetRequestedEvent> context)
    {
        var evt = context.Message;
        var resetLink = $"https://app.notificationplatform.local/reset-password?token={Uri.EscapeDataString(evt.ResetToken)}";

        await _notificationService.SendAsync(new SendNotificationCommand
        {
            UserId = evt.UserId,
            RecipientEmail = evt.Email,
            Type = NotificationType.PasswordReset,
            Channel = NotificationChannel.Email,
            TemplateData = new Dictionary<string, string>
            {
                { "ResetLink", resetLink },
                { "ResetToken", evt.ResetToken },
                { "ExpiresAt", evt.ExpiresAt.ToString("f") }
            }
        }, context.CancellationToken);
    }
}
