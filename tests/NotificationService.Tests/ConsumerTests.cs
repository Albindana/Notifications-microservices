using MassTransit;
using Moq;
using NotificationService.Application.Consumers;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Entities;
using NotificationService.Domain.Enums;
using Shared.Contracts.Events;
using Xunit;

namespace NotificationService.Tests;

public class ConsumerTests
{
    private static Mock<ConsumeContext<T>> ContextFor<T>(T message) where T : class
    {
        var ctx = new Mock<ConsumeContext<T>>();
        ctx.SetupGet(c => c.Message).Returns(message);
        ctx.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);
        return ctx;
    }

    [Fact]
    public async Task UserRegisteredConsumer_CreatesWelcomeNotification()
    {
        var service = new Mock<INotificationService>();
        SendNotificationCommand? captured = null;
        service.Setup(s => s.SendAsync(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SendNotificationCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(new Notification());

        var evt = new UserRegisteredEvent
        {
            UserId = "u-1", Email = "jane@test.com", FirstName = "Jane", RegisteredAt = DateTime.UtcNow
        };

        await new UserRegisteredConsumer(service.Object).Consume(ContextFor(evt).Object);

        Assert.NotNull(captured);
        Assert.Equal(NotificationType.Welcome, captured!.Type);
        Assert.Equal("jane@test.com", captured.RecipientEmail);
        Assert.Equal("Jane", captured.TemplateData["FirstName"]);
    }

    [Fact]
    public async Task PasswordResetConsumer_CreatesPasswordResetNotification_WithResetLink()
    {
        var service = new Mock<INotificationService>();
        SendNotificationCommand? captured = null;
        service.Setup(s => s.SendAsync(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<SendNotificationCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(new Notification());

        var evt = new PasswordResetRequestedEvent
        {
            UserId = "u-1", Email = "jane@test.com", ResetToken = "tok-123", ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        await new PasswordResetRequestedConsumer(service.Object).Consume(ContextFor(evt).Object);

        Assert.NotNull(captured);
        Assert.Equal(NotificationType.PasswordReset, captured!.Type);
        Assert.Equal("tok-123", captured.TemplateData["ResetToken"]);
        Assert.Contains("tok-123", captured.TemplateData["ResetLink"]);
    }
}
