using MediatR;
using UserService.Application.Common.Exceptions;
using UserService.Application.Interfaces;

namespace UserService.Application.Features.Auth.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Unit>
{
    private readonly IIdentityService _identityService;

    public LogoutCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Unit> Handle(LogoutCommand request, CancellationToken ct)
    {
        var user = await _identityService.FindByIdAsync(request.UserId);
        if (user is null)
            throw new AppException("User not found.", 404);

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _identityService.UpdateAsync(user);

        return Unit.Value;
    }
}
