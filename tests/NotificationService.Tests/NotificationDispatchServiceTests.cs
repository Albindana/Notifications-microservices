using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Xunit;

namespace NotificationService.Tests;

public class NotificationDispatchServiceTests
{
    private readonly Mock<INotificationRepository> _notifications = new();
    private readonly Mock<ITemplateRepository> _templates = new();
    private readonly Mock<IEmailSender> _email = new();

    private NotificationDispatchService CreateSut() => new(
        _notifications.Object, _templates.Object, _email.Object,
        NullLogger<NotificationDispatchService>.Instance);

    private static SendNotificationCommand Command() => new()
    {
        UserId = "u-1",
        RecipientEmail = "jane@test.com",
        Type = NotificationType.Welcome,
        Channel = NotificationChannel.Email,
        TemplateData = new Dictionary<string, string> { ["FirstName"] = "Jane" }
    };

    [Fact]
    public async Task SendAsync_RendersTemplate_AndMarksSent_AfterEmailStubCalled()
    {
        _templates.Setup(t => t.GetByTypeAsync(NotificationType.Welcome, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NotificationTemplate
            {
                Type = NotificationType.Welcome,
                Subject = "Welcome {{FirstName}}",
                BodyTemplate = "Hi {{FirstName}}!"
            });

        var result = await CreateSut().SendAsync(Command(), CancellationToken.None);

        Assert.Equal(NotificationStatus.Sent, result.Status);
        Assert.NotNull(result.SentAt);
        Assert.Equal("Welcome Jane", result.Subject);
        Assert.Equal("Hi Jane!", result.Body);
        _email.Verify(e => e.SendAsync("jane@test.com", "Welcome Jane", "Hi Jane!", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_MarksFailed_WhenEmailSenderThrows()
    {
        _templates.Setup(t => t.GetByTypeAsync(It.IsAny<NotificationType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationTemplate?)null); // fallback content
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var result = await CreateSut().SendAsync(Command(), CancellationToken.None);

        Assert.Equal(NotificationStatus.Failed, result.Status);
        Assert.Equal("smtp down", result.ErrorMessage);
        Assert.Null(result.SentAt);
    }

    [Fact]
    public async Task RetryAsync_IncrementsRetryCount_AndResendsFailedNotification()
    {
        var failed = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientEmail = "jane@test.com",
            Subject = "S", Body = "B",
            Status = NotificationStatus.Failed,
            ErrorMessage = "previous failure",
            RetryCount = 0
        };
        _notifications.Setup(r => r.GetByIdAsync(failed.Id, It.IsAny<CancellationToken>())).ReturnsAsync(failed);

        var result = await CreateSut().RetryAsync(failed.Id, CancellationToken.None);

        Assert.Equal(NotificationStatus.Sent, result.Status);
        Assert.Equal(1, result.RetryCount);
        _email.Verify(e => e.SendAsync("jane@test.com", "S", "B", It.IsAny<CancellationToken>()), Times.Once);
    }
}
