using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Notifications.Commands;
using NotificationService.Application.Notifications.Queries;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetNotificationsQuery()));

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
        => Ok(await _mediator.Send(new GetUserNotificationsQuery(userId)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetNotificationByIdQuery(id));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id)
        => Ok(await _mediator.Send(new RetryNotificationCommand(id)));
}
