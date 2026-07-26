using REVORA_BE.Models;

namespace REVORA_BE.Services
{
    public interface IRefreshTokenService
    {
        string GenerateTokenString();
        Task<RefreshToken?> GetRefreshTokenAsync(string tokenString);
        Task RevokeTokenAsync(RefreshToken token);
    }
}
