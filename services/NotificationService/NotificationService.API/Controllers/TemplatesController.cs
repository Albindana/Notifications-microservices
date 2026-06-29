using MediatR;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Templates;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/templates")]
public class TemplatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public TemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _mediator.Send(new GetTemplatesQuery()));

    public record UpdateTemplateRequest(string Subject, string BodyTemplate);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTemplateRequest request)
        => Ok(await _mediator.Send(new UpdateTemplateCommand(id, request.Subject, request.BodyTemplate)));
}
