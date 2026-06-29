using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Features.Auth.Refresh;

public record RefreshTokenCommand : IRequest<AuthResponseDto>
{
    public string RefreshToken { get; init; } = default!;
}
