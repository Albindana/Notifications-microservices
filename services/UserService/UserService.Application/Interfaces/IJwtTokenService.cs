using UserService.Domain.Entities;

namespace UserService.Application.Interfaces;

public interface IJwtTokenService
{
    (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
}
