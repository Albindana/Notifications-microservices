using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Features.Auth.Register;

public record RegisterCommand : IRequest<AuthResponseDto>
{
    public string Email { get; init; } = default!;
    public string Password { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
}
