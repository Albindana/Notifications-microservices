using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;

namespace NotificationService.Application.Notifications.Queries;

// ---- List all (admin) ----
public record GetNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>;

public class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _repository;
    public GetNotificationsQueryHandler(INotificationRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var items = await _repository.ListAsync(ct);
        return items.Select(NotificationDto.FromEntity).ToList();
    }
}

// ---- List by user ----
public record GetUserNotificationsQuery(string UserId) : IRequest<IReadOnlyList<NotificationDto>>;

public class GetUserNotificationsQueryHandler
    : IRequestHandler<GetUserNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly INotificationRepository _repository;
    public GetUserNotificationsQueryHandler(INotificationRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<NotificationDto>> Handle(GetUserNotificationsQuery request, CancellationToken ct)
    {
        var items = await _repository.ListByUserAsync(request.UserId, ct);
        return items.Select(NotificationDto.FromEntity).ToList();
    }
}

// ---- Single by id ----
public record GetNotificationByIdQuery(Guid Id) : IRequest<NotificationDto?>;

public class GetNotificationByIdQueryHandler
    : IRequestHandler<GetNotificationByIdQuery, NotificationDto?>
{
    private readonly INotificationRepository _repository;
    public GetNotificationByIdQueryHandler(INotificationRepository repository) => _repository = repository;

    public async Task<NotificationDto?> Handle(GetNotificationByIdQuery request, CancellationToken ct)
    {
        var item = await _repository.GetByIdAsync(request.Id, ct);
        return item is null ? null : NotificationDto.FromEntity(item);
    }
}
