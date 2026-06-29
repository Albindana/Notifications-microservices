using MediatR;

namespace UserService.Application.Features.Users.ForgotPassword;

public record ForgotPasswordCommand : IRequest<Unit>
{
    public string Email { get; init; } = default!;
}
