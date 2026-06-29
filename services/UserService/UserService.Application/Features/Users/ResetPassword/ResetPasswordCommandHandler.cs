using MediatR;
using UserService.Application.Common.Exceptions;
using UserService.Application.Interfaces;

namespace UserService.Application.Features.Users.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var user = await _identityService.FindByEmailAsync(request.Email);
        if (user is null)
            throw new AppException("Invalid password reset request.", 400);

        var (succeeded, errors) = await _identityService.ResetPasswordAsync(
            user, request.Token, request.NewPassword);

        if (!succeeded)
            throw new AppException(string.Join(" ", errors), 400);

        return Unit.Value;
    }
}
