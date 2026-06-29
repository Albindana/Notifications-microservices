using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence;

namespace UserService.Infrastructure.Services;

public class IdentityService : IIdentityService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _dbContext;

    public IdentityService(UserManager<AppUser> userManager, AppDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
    }

    public Task<AppUser?> FindByEmailAsync(string email) =>
        _userManager.FindByEmailAsync(email)!;

    public Task<AppUser?> FindByIdAsync(string userId) =>
        _userManager.FindByIdAsync(userId)!;

    public Task<AppUser?> FindByRefreshTokenAsync(string refreshToken) =>
        _dbContext.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

    public async Task<(bool Succeeded, AppUser? User, string[] Errors)> CreateUserAsync(
        string email, string firstName, string lastName, string password)
    {
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        return result.Succeeded
            ? (true, user, Array.Empty<string>())
            : (false, null, result.Errors.Select(e => e.Description).ToArray());
    }

    public Task<bool> CheckPasswordAsync(AppUser user, string password) =>
        _userManager.CheckPasswordAsync(user, password);

    public async Task UpdateAsync(AppUser user)
    {
        await _userManager.UpdateAsync(user);
    }

    public async Task<(bool Succeeded, string[] Errors)> UpdateProfileAsync(
        AppUser user, string firstName, string lastName)
    {
        user.FirstName = firstName;
        user.LastName = lastName;
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? (true, Array.Empty<string>())
            : (false, result.Errors.Select(e => e.Description).ToArray());
    }

    public Task<string> GeneratePasswordResetTokenAsync(AppUser user) =>
        _userManager.GeneratePasswordResetTokenAsync(user);

    public async Task<(bool Succeeded, string[] Errors)> ResetPasswordAsync(
        AppUser user, string token, string newPassword)
    {
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded
            ? (true, Array.Empty<string>())
            : (false, result.Errors.Select(e => e.Description).ToArray());
    }
}
