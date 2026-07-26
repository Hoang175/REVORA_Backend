using Microsoft.EntityFrameworkCore;
using REVORA_BE.Models;
using System.Security.Cryptography;

namespace REVORA_BE.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly AppDbContext _context;

        public RefreshTokenService(AppDbContext context)
        {
            _context = context;
        }

        public string GenerateTokenString()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string tokenString)
        {
            return await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == tokenString);
        }

        public Task RevokeTokenAsync(RefreshToken token)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            _context.RefreshTokens.Update(token);
            return Task.CompletedTask;
        }
    }
}
