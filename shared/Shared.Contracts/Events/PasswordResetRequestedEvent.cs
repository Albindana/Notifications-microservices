namespace Shared.Contracts.Events;

public record PasswordResetRequestedEvent
{
    public string UserId { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string ResetToken { get; init; } = default!;
    public DateTime ExpiresAt { get; init; }
}
