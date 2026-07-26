using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using REVORA_BE.Configs;
using REVORA_BE.DTOs;
using REVORA_BE.Exceptions;
using REVORA_BE.Services;
using System.Security.Claims;

namespace REVORA_BE.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly JwtSettings _jwtSettings;

        public AuthController(
            IAuthService authService, 
            IOptions<JwtSettings> jwtSettings)
        {
            _authService = authService;
            _jwtSettings = jwtSettings.Value;
        }

        [HttpPost("register/send-link")]
        public async Task<IActionResult> SendRegistrationLink([FromBody] SendRegisterOtpRequestDto dto, CancellationToken ct)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var verificationUrlTemplate = $"{baseUrl}/api/v1/auth/register/verify-link?email={{email}}&token={{token}}";
            await _authService.SendRegistrationLinkAsync(dto.Email, verificationUrlTemplate, ct);
            return Ok(ApiResponse<object>.Ok(null, "Link xác thực đã được gửi đến email của bạn."));
        }

        [HttpGet("register/verify-link")]
        public async Task<IActionResult> VerifyRegistrationLink([FromQuery] string email, [FromQuery] string token, CancellationToken ct)
        {
            bool isValid = await _authService.VerifyLinkAsync(email, token, ct);
            
            var html = isValid 
                ? @"<html><body style='font-family:sans-serif; text-align:center; padding-top:50px; color:#15803d;'>
                        <h2>Xác nhận Email thành công!</h2>
                        <p>Bạn có thể quay lại trang đăng ký ban đầu để tiếp tục.</p>
                        <script>setTimeout(function(){ window.close(); }, 3000);</script>
                    </body></html>"
                : @"<html><body style='font-family:sans-serif; text-align:center; padding-top:50px; color:#b91c1c;'>
                        <h2>Link không hợp lệ hoặc đã hết hạn!</h2>
                        <p>Vui lòng yêu cầu gửi lại link xác thực mới.</p>
                    </body></html>";

            return Content(html, "text/html", System.Text.Encoding.UTF8);
        }

        [HttpGet("register/check-status")]
        public async Task<IActionResult> CheckVerificationStatus([FromQuery] string email, CancellationToken ct)
        {
            bool isVerified = await _authService.CheckVerificationStatusAsync(email, ct);
            return Ok(ApiResponse<object>.Ok(new { verified = isVerified }));
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
        {
            await _authService.RegisterAsync(dto, ct);
            return Ok(ApiResponse<object>.Ok(null, "Registration successful. Please login."));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
        {
            var tokenDto = await _authService.LoginAsync(dto, ct);

            SetRefreshTokenCookie(tokenDto.RefreshToken, tokenDto.RefreshTokenExpiresAt);

            return Ok(ApiResponse<object>.Ok(new
            {
                accessToken = tokenDto.AccessToken,
                expiresAt = tokenDto.AccessTokenExpiresAt,
                isFirstLogin = tokenDto.IsFirstLogin
            }, "Login successful."));
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] REVORA_BE.DTOs.Request.GoogleLoginRequestDto dto, CancellationToken ct)
        {
            var tokenDto = await _authService.GoogleLoginAsync(dto, ct);

            SetRefreshTokenCookie(tokenDto.RefreshToken, tokenDto.RefreshTokenExpiresAt);

            return Ok(ApiResponse<object>.Ok(new
            {
                accessToken = tokenDto.AccessToken,
                expiresAt = tokenDto.AccessTokenExpiresAt,
                isFirstLogin = tokenDto.IsFirstLogin
            }, "Google login successful."));
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(CancellationToken ct)
        {
            if (!Request.Cookies.TryGetValue(_jwtSettings.RefreshTokenCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            {
                throw new UnauthorizedException("Refresh token is missing.", "MissingRefreshTokenCookie");
            }

            var tokenDto = await _authService.RefreshAsync(refreshToken, ct);

            SetRefreshTokenCookie(tokenDto.RefreshToken, tokenDto.RefreshTokenExpiresAt);

            return Ok(ApiResponse<object>.Ok(new
            {
                accessToken = tokenDto.AccessToken,
                expiresAt = tokenDto.AccessTokenExpiresAt
            }, "Token refreshed successfully."));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken ct)
        {
            if (Request.Cookies.TryGetValue(_jwtSettings.RefreshTokenCookieName, out var refreshToken))
            {
                await _authService.LogoutAsync(refreshToken, ct);
            }

            RemoveRefreshTokenCookie();
            return Ok(ApiResponse<object>.Ok(null, "Logout successful."));
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll(CancellationToken ct)
        {
            var userId = GetUserId();
            await _authService.LogoutAllAsync(userId, ct);

            RemoveRefreshTokenCookie();
            return Ok(ApiResponse<object>.Ok(null, "Log out from all devices successful."));
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
        {
            var userId = GetUserId();

            await _authService.ChangePasswordAsync(userId, dto, ct);
            
            // Delete RefreshToken from cookies using standard helper
            RemoveRefreshTokenCookie();

            return Ok(ApiResponse<object>.Ok(null, "Password changed successfully. Please login again."));
        }

        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> SendResetPasswordOtp([FromBody] REVORA_BE.DTOs.Request.SendOtpRequestDto request, CancellationToken ct)
        {
            await _authService.SendResetPasswordOtpAsync(request.Email, ct);
            return Ok(ApiResponse<object>.Ok(null, "Mã OTP đã được gửi đến email của bạn."));
        }

        [HttpPost("forgot-password/verify-otp")]
        public async Task<IActionResult> VerifyResetPasswordOtp([FromBody] REVORA_BE.DTOs.Request.VerifyOtpRequestDto request, CancellationToken ct)
        {
            var isValid = await _authService.VerifyResetPasswordOtpAsync(request.Email, request.Otp, ct);
            if (!isValid)
            {
                throw new ValidationException("Mã OTP không hợp lệ hoặc đã hết hạn.");
            }
            return Ok(ApiResponse<object>.Ok(null, "Xác thực OTP thành công."));
        }

        [HttpPost("forgot-password/reset-password")]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] REVORA_BE.DTOs.Request.ResetPasswordRequestDto request, CancellationToken ct)
        {
            await _authService.ResetPasswordWithOtpAsync(request.Email, request.Otp, request.NewPassword, ct);
            return Ok(ApiResponse<object>.Ok(null, "Đặt lại mật khẩu thành công."));
        }

        // --- Helper Methods ---

        private long GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("User identification invalid.", "InvalidUserClaim");
            }
            return userId;
        }

        private void SetRefreshTokenCookie(string token, DateTime expiresAt)
        {
            if (!Enum.TryParse<SameSiteMode>(_jwtSettings.RefreshTokenCookieSameSite, out var sameSiteMode))
            {
                sameSiteMode = SameSiteMode.Lax;
            }

            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = _jwtSettings.RefreshTokenCookieSecure,
                SameSite = sameSiteMode,
                Expires = expiresAt,
                Path = "/"
            };

            // Chỉ set domain nếu không phải là localhost để tránh lỗi trình duyệt reject cookie
            var host = Request.Host.Host;
            if (!host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                options.Domain = ".revora.io.vn";
            }

            Response.Cookies.Append(_jwtSettings.RefreshTokenCookieName, token, options);
        }

        private void RemoveRefreshTokenCookie()
        {
            if (!Enum.TryParse<SameSiteMode>(_jwtSettings.RefreshTokenCookieSameSite, out var sameSiteMode))
            {
                sameSiteMode = SameSiteMode.Lax;
            }

            var options = new CookieOptions
            {
                HttpOnly = true,
                Secure = _jwtSettings.RefreshTokenCookieSecure,
                SameSite = sameSiteMode,
                Expires = DateTime.UtcNow.AddDays(-1),
                Path = "/"
            };

            // Phải xóa đúng domain như lúc tạo
            var host = Request.Host.Host;
            if (!host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                options.Domain = ".revora.io.vn";
            }

            Response.Cookies.Delete(_jwtSettings.RefreshTokenCookieName, options);
        }
    }
}
