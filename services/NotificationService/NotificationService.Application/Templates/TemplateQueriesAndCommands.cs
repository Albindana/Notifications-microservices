using MediatR;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;

namespace NotificationService.Application.Templates;

// ---- List templates (admin) ----
public record GetTemplatesQuery : IRequest<IReadOnlyList<NotificationTemplateDto>>;

public class GetTemplatesQueryHandler
    : IRequestHandler<GetTemplatesQuery, IReadOnlyList<NotificationTemplateDto>>
{
    private readonly ITemplateRepository _repository;
    public GetTemplatesQueryHandler(ITemplateRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<NotificationTemplateDto>> Handle(GetTemplatesQuery request, CancellationToken ct)
    {
        var items = await _repository.ListAsync(ct);
        return items.Select(NotificationTemplateDto.FromEntity).ToList();
    }
}

// ---- Update template body/subject (admin) ----
public record UpdateTemplateCommand(Guid Id, string Subject, string BodyTemplate)
    : IRequest<NotificationTemplateDto>;

public class UpdateTemplateCommandHandler : IRequestHandler<UpdateTemplateCommand, NotificationTemplateDto>
{
    private readonly ITemplateRepository _repository;
    public UpdateTemplateCommandHandler(ITemplateRepository repository) => _repository = repository;

    public async Task<NotificationTemplateDto> Handle(UpdateTemplateCommand request, CancellationToken ct)
    {
        var template = await _repository.GetByIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException($"Template {request.Id} not found.");

        template.Subject = request.Subject;
        template.BodyTemplate = request.BodyTemplate;
        template.UpdatedAt = DateTime.UtcNow;

        await _repository.SaveChangesAsync(ct);
        return NotificationTemplateDto.FromEntity(template);
    }
}
