using Moq;
using UserService.Application.Common.Exceptions;
using UserService.Application.Features.Auth.Refresh;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using Xunit;

namespace UserService.Tests;

public class RefreshTokenHandlerTests
{
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IJwtTokenService> _jwt = new();

    private RefreshTokenCommandHandler CreateSut() => new(_identity.Object, _jwt.Object);

    [Fact]
    public async Task Handle_RotatesToken_OnValidRefreshToken()
    {
        var user = new AppUser
        {
            Id = "user-1",
            Email = "u@test.com",
            RefreshToken = "old-token",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(1)
        };
        _identity.Setup(x => x.FindByRefreshTokenAsync("old-token")).ReturnsAsync(user);
        _jwt.Setup(x => x.GenerateAccessToken(user)).Returns(("new-access", DateTime.UtcNow.AddHours(1)));
        _jwt.Setup(x => x.GenerateRefreshToken()).Returns("new-token");

        var result = await CreateSut().Handle(
            new RefreshTokenCommand { RefreshToken = "old-token" }, CancellationToken.None);

        Assert.Equal("new-access", result.AccessToken);
        Assert.Equal("new-token", result.RefreshToken);
        // The token must be rotated to a fresh value.
        _identity.Verify(x => x.UpdateAsync(It.Is<AppUser>(u => u.RefreshToken == "new-token")), Times.Once);
    }

    [Fact]
    public async Task Handle_Rejects_ExpiredToken()
    {
        var user = new AppUser
        {
            Id = "user-1",
            RefreshToken = "old-token",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1)
        };
        _identity.Setup(x => x.FindByRefreshTokenAsync("old-token")).ReturnsAsync(user);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            CreateSut().Handle(new RefreshTokenCommand { RefreshToken = "old-token" }, CancellationToken.None));

        Assert.Equal(401, ex.StatusCode);
        _jwt.Verify(x => x.GenerateAccessToken(It.IsAny<AppUser>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Rejects_UnknownToken()
    {
        _identity.Setup(x => x.FindByRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            CreateSut().Handle(new RefreshTokenCommand { RefreshToken = "nope" }, CancellationToken.None));

        Assert.Equal(401, ex.StatusCode);
    }
}
