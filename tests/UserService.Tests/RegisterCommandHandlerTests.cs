using MassTransit;
using Moq;
using Shared.Contracts.Events;
using UserService.Application.Common.Exceptions;
using UserService.Application.Features.Auth.Register;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.Tests;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<IPublishEndpoint> _publish = new();

    private RegisterCommandHandler CreateSut() => new(_identity.Object, _jwt.Object, _publish.Object);

    private static RegisterCommand ValidCommand() => new()
    {
        Email = "new@test.com",
        Password = "Passw0rd",
        FirstName = "New",
        LastName = "User"
    };

    [Fact]
    public async Task Handle_RegistersUser_AndReturnsTokens()
    {
        var user = new AppUser { Id = "user-1", Email = "new@test.com", FirstName = "New", LastName = "User" };
        _identity.Setup(x => x.FindByEmailAsync("new@test.com")).ReturnsAsync((AppUser?)null);
        _identity.Setup(x => x.CreateUserAsync("new@test.com", "New", "User", "Passw0rd"))
            .ReturnsAsync((true, user, Array.Empty<string>()));
        _jwt.Setup(x => x.GenerateAccessToken(user)).Returns(("access-token", DateTime.UtcNow.AddHours(1)));
        _jwt.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");

        var result = await CreateSut().Handle(ValidCommand(), CancellationToken.None);

        Assert.Equal("access-token", result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        _identity.Verify(x => x.UpdateAsync(It.Is<AppUser>(u => u.RefreshToken == "refresh-token")), Times.Once);
    }

    [Fact]
    public async Task Handle_PublishesUserRegisteredEvent()
    {
        var user = new AppUser { Id = "user-1", Email = "new@test.com", FirstName = "New", LastName = "User" };
        _identity.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        _identity.Setup(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((true, user, Array.Empty<string>()));
        _jwt.Setup(x => x.GenerateAccessToken(It.IsAny<AppUser>())).Returns(("a", DateTime.UtcNow));
        _jwt.Setup(x => x.GenerateRefreshToken()).Returns("r");

        await CreateSut().Handle(ValidCommand(), CancellationToken.None);

        _publish.Verify(p => p.Publish(
            It.Is<UserRegisteredEvent>(e => e.UserId == "user-1" && e.Email == "new@test.com" && e.FirstName == "New"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectsDuplicateEmail()
    {
        _identity.Setup(x => x.FindByEmailAsync("new@test.com"))
            .ReturnsAsync(new AppUser { Id = "existing", Email = "new@test.com" });

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            CreateSut().Handle(ValidCommand(), CancellationToken.None));

        Assert.Equal(409, ex.StatusCode);
        _identity.Verify(x => x.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _publish.Verify(p => p.Publish(It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
