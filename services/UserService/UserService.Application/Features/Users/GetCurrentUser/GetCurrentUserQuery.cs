using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Features.Users.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserProfileDto>
{
    public string UserId { get; init; } = default!;
}
