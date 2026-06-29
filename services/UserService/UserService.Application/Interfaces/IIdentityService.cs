using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

/// <summary>
/// Abstraction over ASP.NET Core Identity's UserManager so Application-layer
/// handlers stay free of infrastructure concerns and remain unit-testable.
/// </summary>
public interface IIdentityService
{
    Task<AppUser?> FindByEmailAsync(string email);
    Task<AppUser?> FindByIdAsync(string userId);
    Task<AppUser?> FindByRefreshTokenAsync(string refreshToken);
    Task<(bool Succeeded, AppUser? User, string[] Errors)> CreateUserAsync(
        string email, string firstName, string lastName, string password);
    Task<bool> CheckPasswordAsync(AppUser user, string password);
    Task UpdateAsync(AppUser user);
    Task<(bool Succeeded, string[] Errors)> UpdateProfileAsync(
        AppUser user, string firstName, string lastName);
    Task<string> GeneratePasswordResetTokenAsync(AppUser user);
    Task<(bool Succeeded, string[] Errors)> ResetPasswordAsync(
        AppUser user, string token, string newPassword);
}
