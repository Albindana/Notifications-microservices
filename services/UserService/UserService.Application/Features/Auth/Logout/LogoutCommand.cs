using MediatR;

namespace UserService.Application.Features.Auth.Logout;

public record LogoutCommand : IRequest<Unit>
{
    public string UserId { get; init; } = default!;
}
