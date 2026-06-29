using MassTransit;
using MediatR;
using Shared.Contracts.Events;
using UserService.Application.Common.Exceptions;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;

namespace UserService.Application.Features.Auth.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IIdentityService _identityService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPublishEndpoint _publishEndpoint;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IJwtTokenService jwtTokenService,
        IPublishEndpoint publishEndpoint)
    {
        _identityService = identityService;
        _jwtTokenService = jwtTokenService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken ct)
    {
        var existing = await _identityService.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new AppException("A user with this email already exists.", 409);

        var (succeeded, user, errors) = await _identityService.CreateUserAsync(
            request.Email, request.FirstName, request.LastName, request.Password);

        if (!succeeded || user is null)
            throw new AppException(string.Join(" ", errors), 400);

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _identityService.UpdateAsync(user);

        await _publishEndpoint.Publish(new UserRegisteredEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            RegisteredAt = DateTime.UtcNow
        }, ct);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };
    }
}
