using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure.Services;

/// <summary>
/// Email stub: logs the message instead of sending it. The integration point is clean
/// and swappable — replace with a real provider (SendGrid, SES, SMTP) without touching callers.
/// </summary>
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[EMAIL STUB] To: {To} | Subject: {Subject} | Body: {Body}",
            to, subject, body);

        return Task.CompletedTask;
    }
}
