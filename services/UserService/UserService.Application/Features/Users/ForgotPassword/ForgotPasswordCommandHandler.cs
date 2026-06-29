using MassTransit;
using MediatR;
using Shared.Contracts.Events;
using UserService.Application.Interfaces;

namespace UserService.Application.Features.Users.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly IIdentityService _identityService;
    private readonly IPublishEndpoint _publishEndpoint;

    public ForgotPasswordCommandHandler(IIdentityService identityService, IPublishEndpoint publishEndpoint)
    {
        _identityService = identityService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _identityService.FindByEmailAsync(request.Email);

        // Do not reveal whether the email exists — silently succeed if it doesn't.
        if (user is null)
            return Unit.Value;

        var resetToken = await _identityService.GeneratePasswordResetTokenAsync(user);
        var expiresAt = DateTime.UtcNow.AddHours(1);

        await _publishEndpoint.Publish(new PasswordResetRequestedEvent
        {
            UserId = user.Id,
            Email = user.Email!,
            ResetToken = resetToken,
            ExpiresAt = expiresAt
        }, ct);

        return Unit.Value;
    }
}
