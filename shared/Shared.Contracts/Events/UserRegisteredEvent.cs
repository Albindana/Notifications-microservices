namespace Shared.Contracts.Events;

public record UserRegisteredEvent
{
    public string UserId { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public DateTime RegisteredAt { get; init; }
}
