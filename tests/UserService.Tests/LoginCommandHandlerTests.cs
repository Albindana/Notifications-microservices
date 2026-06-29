using Moq;
using UserService.Application.Common.Exceptions;
using UserService.Application.Features.Auth.Login;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.Tests;

public class LoginCommandHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IJwtTokenService> _jwt = new();

    private LoginCommandHandler CreateSut() => new(_identity.Object, _jwt.Object);

    [Fact]
    public async Task Handle_ReturnsTokens_OnValidCredentials()
    {
        var user = new AppUser { Id = "user-1", Email = "u@test.com", FirstName = "U", LastName = "Ser" };
        _identity.Setup(x => x.FindByEmailAsync("u@test.com")).ReturnsAsync(user);
        _identity.Setup(x => x.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
        _jwt.Setup(x => x.GenerateAccessToken(user)).Returns(("access", DateTime.UtcNow.AddHours(1)));
        _jwt.Setup(x => x.GenerateRefreshToken()).Returns("refresh");

        var result = await CreateSut().Handle(
            new LoginCommand { Email = "u@test.com", Password = "correct" }, CancellationToken.None);

        Assert.Equal("access", result.AccessToken);
        Assert.Equal("refresh", result.RefreshToken);
        _identity.Verify(x => x.UpdateAsync(It.Is<AppUser>(u => u.RefreshToken == "refresh")), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_OnWrongPassword()
    {
        var user = new AppUser { Id = "user-1", Email = "u@test.com" };
        _identity.Setup(x => x.FindByEmailAsync("u@test.com")).ReturnsAsync(user);
        _identity.Setup(x => x.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            CreateSut().Handle(new LoginCommand { Email = "u@test.com", Password = "wrong" }, CancellationToken.None));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task Handle_Throws_WhenUserNotFound()
    {
        _identity.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            CreateSut().Handle(new LoginCommand { Email = "missing@test.com", Password = "x" }, CancellationToken.None));

        Assert.Equal(401, ex.StatusCode);
    }
}
