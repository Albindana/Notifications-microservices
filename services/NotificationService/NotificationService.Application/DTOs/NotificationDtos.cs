using NotificationService.Domain.Entities;

namespace NotificationService.Application.DTOs;

public record NotificationDto
{
    public Guid Id { get; init; }
    public string UserId { get; init; } = default!;
    public string RecipientEmail { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string Channel { get; init; } = default!;
    public string Subject { get; init; } = default!;
    public string Body { get; init; } = default!;
    public string Status { get; init; } = default!;
    public DateTime? SentAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }

    public static NotificationDto FromEntity(Notification n) => new()
    {
        Id = n.Id,
        UserId = n.UserId,
        RecipientEmail = n.RecipientEmail,
        Type = n.Type.ToString(),
        Channel = n.Channel.ToString(),
        Subject = n.Subject,
        Body = n.Body,
        Status = n.Status.ToString(),
        SentAt = n.SentAt,
        CreatedAt = n.CreatedAt,
        ErrorMessage = n.ErrorMessage,
        RetryCount = n.RetryCount
    };
}

public record NotificationTemplateDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = default!;
    public string Subject { get; init; } = default!;
    public string BodyTemplate { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    public static NotificationTemplateDto FromEntity(NotificationTemplate t) => new()
    {
        Id = t.Id,
        Type = t.Type.ToString(),
        Subject = t.Subject,
        BodyTemplate = t.BodyTemplate,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
