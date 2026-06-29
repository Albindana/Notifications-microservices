using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;

namespace NotificationService.Application.Notifications.Commands;

public record RetryNotificationCommand(Guid Id) : IRequest<NotificationDto>;

public class RetryNotificationCommandHandler : IRequestHandler<RetryNotificationCommand, NotificationDto>
{
    private readonly INotificationService _notificationService;

    public RetryNotificationCommandHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<NotificationDto> Handle(RetryNotificationCommand request, CancellationToken ct)
    {
        var notification = await _notificationService.RetryAsync(request.Id, ct);
        return NotificationDto.FromEntity(notification);
    }
}
