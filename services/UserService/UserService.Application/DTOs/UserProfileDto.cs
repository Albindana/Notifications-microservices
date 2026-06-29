namespace UserService.Application.DTOs;

public record UserProfileDto
{
    public string Id { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string FirstName { get; init; } = default!;
    public string LastName { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
}
