namespace Shared.Contracts.Events;

public record UserProfileUpdatedEvent
{
    public string UserId { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
}
