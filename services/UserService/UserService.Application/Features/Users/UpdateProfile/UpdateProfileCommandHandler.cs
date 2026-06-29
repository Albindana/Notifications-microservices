using MassTransit;
using MediatR;
using Shared.Contracts.Events;
using UserService.Application.Common.Exceptions;
using UserService.Application.DTOs;
using UserService.Application.Interfaces;

namespace UserService.Application.Features.Users.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    private readonly IIdentityService _identityService;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateProfileCommandHandler(IIdentityService identityService, IPublishEndpoint publishEndpoint)
    {
        _identityService = identityService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var user = await _identityService.FindByIdAsync(request.UserId);
        if (user is null)
            throw new AppException("User not found.", 404);

        var (succeeded, errors) = await _identityService.UpdateProfileAsync(
            user, request.FirstName, request.LastName);

        if (!succeeded)
            throw new AppException(string.Join(" ", errors), 400);

        await _publishEndpoint.Publish(new UserProfileUpdatedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName
        }, ct);

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt
        };
    }
}
