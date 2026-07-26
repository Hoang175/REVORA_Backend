using System.ComponentModel.DataAnnotations;

namespace REVORA_BE.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string City { get; set; }
    }

    public class SendRegisterOtpRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }

    public class LoginDto
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }

    // Standardized Success Response Wrapper
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public ApiResponse(T? data, string message = "Success")
        {
            Data = data;
            Message = message;
        }

        public static ApiResponse<T> Ok(T? data, string message = "Success") => new(data, message);
    }

    public class TokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        public bool IsFirstLogin { get; set; }
    }

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@#$!%*?&]).{8,}$",
            ErrorMessage = "Mật khẩu phải chứa ít nhất 1 chữ hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt (@#$!%*?&).")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
        public string ConfirmPassword { get; set; }
    }

    public class SessionInfoDto
    {
        public long TokenId { get; set; }
        public string DeviceName { get; set; } = null!;
        public string IpAddress { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }

    public sealed class UserProfileDto
    {
        public long UserId { get; init; }

        public string Username { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string FullName { get; init; } = string.Empty;

        public string? Phone { get; init; }

        public string? AvatarUrl { get; init; }

        public string? Bio { get; init; }

        public DateTime? Birthday { get; init; }

        public string? Gender { get; init; }

        public string? Address { get; init; }

        public string? City { get; init; }

        public DateTime CreatedAt { get; init; }

        public int? SoldCount { get; init; }

        public int? SellingCount { get; init; }

        public int? FollowerCount { get; init; }

        public int? FollowingCount { get; init; }

        public bool IsFollowing { get; init; }
        
        public int? BadgeId { get; init; }
        
        public REVORA_BE.DTOs.Response.BadgeResponseDto? Badge { get; init; }
    }

    public sealed class UserSummaryDto
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Bio { get; set; }
        public int FollowerCount { get; set; }
        public bool IsFollowing { get; set; }
    }

    public sealed class UpdateProfileDto
    {
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        public string FullName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        public string? Phone { get; set; }

        public DateTime? Birthday { get; set; }

        [StringLength(20, ErrorMessage = "Gender cannot exceed 20 characters.")]
        public string? Gender { get; set; }

        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
        public string? Address { get; set; }

        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters.")]
        public string? City { get; set; }

        [StringLength(500, ErrorMessage = "Bio cannot exceed 500 characters.")]
        public string? Bio { get; set; }

        public string? AvatarUrl { get; set; }
    }

    public sealed class UpdateUserBadgeRequestDto
    {
        public int? BadgeId { get; set; }
    }
}
