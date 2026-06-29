using MassTransit;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Enums;
using Shared.Contracts.Events;

namespace NotificationService.Application.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegisteredEvent>
{
    private readonly INotificationService _notificationService;

    public UserRegisteredConsumer(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        var evt = context.Message;

        await _notificationService.SendAsync(new SendNotificationCommand
        {
            UserId = evt.UserId,
            RecipientEmail = evt.Email,
            Type = NotificationType.Welcome,
            Channel = NotificationChannel.Email,
            TemplateData = new Dictionary<string, string>
            {
                { "FirstName", evt.FirstName },
                { "RegisteredAt", evt.RegisteredAt.ToString("f") }
            }
        }, context.CancellationToken);
    }
}
