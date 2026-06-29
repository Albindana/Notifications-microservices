using MediatR;
using UserService.Application.DTOs;

namespace UserService.Application.Features.Users.UpdateProfile;

public record UpdateProfileCommand : IRequest<UserProfileDto>
{
    public string UserId { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
}
