using MediatR;

namespace UserService.Application.Features.Users.ResetPassword;

public record ResetPasswordCommand : IRequest<Unit>
{
    public string Email { get; init; } = default!;
    public string Token { get; init; } = default!;
    public string NewPassword { get; init; } = default!;
}
