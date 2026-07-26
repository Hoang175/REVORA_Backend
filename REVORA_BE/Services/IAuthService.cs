using REVORA_BE.DTOs;

namespace REVORA_BE.Services
{
    public interface IAuthService
    {
        Task SendRegistrationLinkAsync(string email, string verificationUrlTemplate, CancellationToken cancellationToken = default);
        Task<bool> VerifyLinkAsync(string email, string token, CancellationToken cancellationToken = default);
        Task<bool> CheckVerificationStatusAsync(string email, CancellationToken cancellationToken = default);
        Task RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
        Task<TokenDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);
        Task<TokenDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
        Task LogoutAllAsync(long userId, CancellationToken cancellationToken = default);
        Task ChangePasswordAsync(long userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);
        Task<TokenDto> GoogleLoginAsync(REVORA_BE.DTOs.Request.GoogleLoginRequestDto request, CancellationToken cancellationToken = default);
        Task SendResetPasswordOtpAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> VerifyResetPasswordOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
        Task ResetPasswordWithOtpAsync(string email, string otp, string newPassword, CancellationToken cancellationToken = default);
    }
}
