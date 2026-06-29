using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Features.Auth.Login;

public record LoginCommand : IRequest<AuthResponseDto>
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
}
