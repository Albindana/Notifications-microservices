using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NotificationService.Application.Consumers;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Notifications;
using NotificationService.Domain.Entities;
using Shared.Contracts.Events;
using Xunit;

namespace NotificationService.Tests;

/// <summary>
/// End-to-end publish → consume flow using MassTransit's in-memory test harness
/// (the MassTransit 8 equivalent of the legacy InMemoryTestHarness).
/// </summary>
public class MassTransitHarnessTests
{
    [Fact]
    public async Task WhenUserRegisters_NotificationServiceReceivesEvent()
    {
        var notificationService = new Mock<INotificationService>();
        notificationService
            .Setup(s => s.SendAsync(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Notification());

        await using var provider = new ServiceCollection()
            .AddSingleton(notificationService.Object)
            .AddMassTransitTestHarness(x => x.AddConsumer<UserRegisteredConsumer>())
            .BuildServiceProvider(true);

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await harness.Bus.Publish(new UserRegisteredEvent
            {
                UserId = "user-1",
                Email = "test@example.com",
                FirstName = "Test",
                RegisteredAt = DateTime.UtcNow
            });

            Assert.True(await harness.Consumed.Any<UserRegisteredEvent>());

            var consumerHarness = harness.GetConsumerHarness<UserRegisteredConsumer>();
            Assert.True(await consumerHarness.Consumed.Any<UserRegisteredEvent>());

            notificationService.Verify(
                s => s.SendAsync(It.IsAny<SendNotificationCommand>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            await harness.Stop();
        }
    }
}
