using ECommerceAPI.Domain.Entities;

namespace ECommerceAPI.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    Guid? GetUserIdFromExpiredToken(string token);
}
