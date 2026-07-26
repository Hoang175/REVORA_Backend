using System.Security.Claims;
using REVORA_BE.Models;

namespace REVORA_BE.Services
{
    public interface IJwtService
    {
        string GenerateAccessToken(User user, IEnumerable<string> permissions);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
