using MassTransit;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Enums;
using Shared.Contracts.Events;

namespace NotificationService.Application.Consumers;

public class UserProfileUpdatedConsumer : IConsumer<UserProfileUpdatedEvent>
{
    private readonly INotificationService _notificationService;

    public UserProfileUpdatedConsumer(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task Consume(ConsumeContext<UserProfileUpdatedEvent> context)
    {
        var evt = context.Message;

        await _notificationService.SendAsync(new SendNotificationCommand
        {
            UserId = evt.UserId,
            RecipientEmail = evt.Email,
            Type = NotificationType.ProfileUpdate,
            Channel = NotificationChannel.Email,
            TemplateData = new Dictionary<string, string>
            {
                { "FirstName", evt.FirstName },
                { "LastName", evt.LastName }
            }
        }, context.CancellationToken);
    }
}
