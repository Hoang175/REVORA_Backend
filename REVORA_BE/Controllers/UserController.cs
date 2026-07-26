using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using REVORA_BE.DTOs;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Exceptions;
using REVORA_BE.Services;
using System.Security.Claims;

namespace REVORA_BE.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var userId = GetUserId();

            var profile = await _userService.GetMyProfileAsync(userId, ct);

            return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile retrieved successfully."));
        }

        [Authorize]
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto, CancellationToken ct)
        {
            var userId = GetUserId();

            var updatedProfile = await _userService.UpdateProfileAsync(userId, dto, ct);

            return Ok(ApiResponse<UserProfileDto>.Ok(updatedProfile, "Profile updated successfully."));
        }

        [Authorize]
        [HttpGet("badges")]
        public async Task<IActionResult> GetBadges(CancellationToken ct)
        {
            long userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var badges = await _userService.GetBadgesAsync(userId, ct);
            return Ok(ApiResponse<List<REVORA_BE.DTOs.Response.BadgeResponseDto>>.Ok(badges, "Badges retrieved successfully."));
        }

        [Authorize]
        [HttpPut("me/badge")]
        public async Task<IActionResult> UpdateBadge([FromBody] UpdateUserBadgeRequestDto dto, CancellationToken ct)
        {
            var userId = GetUserId();
            var updatedProfile = await _userService.UpdateBadgeAsync(userId, dto.BadgeId, ct);
            return Ok(ApiResponse<UserProfileDto>.Ok(updatedProfile, "Badge updated successfully."));
        }

        [HttpGet("{userId:long}")]
        public async Task<IActionResult> GetUserProfile(long userId, CancellationToken ct)
        {
            var currentUserId = GetUserIdOptional();
            var profile = await _userService.GetUserProfileAsync(userId, currentUserId, ct);

            return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profile retrieved successfully."));
        }

        [Authorize]
        [HttpPost("{userId:long}/toggle-follow")]
        public async Task<IActionResult> ToggleFollow(long userId, CancellationToken ct)
        {
            var currentUserId = GetUserId();
            var isFollowing = await _userService.ToggleFollowAsync(currentUserId, userId, ct);
            var message = isFollowing ? "Successfully followed the user." : "Successfully unfollowed the user.";
            return Ok(ApiResponse<object>.Ok(new { IsFollowing = isFollowing }, message));
        }

        [HttpGet("{userId:long}/followers")]
        public async Task<IActionResult> GetFollowers(long userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var currentUserId = GetUserIdOptional();
            var result = await _userService.GetFollowersAsync(userId, currentUserId, pageNumber, pageSize, ct);
            return Ok(ApiResponse<PagedResult<UserSummaryDto>>.Ok(result, "Followers retrieved successfully."));
        }

        [HttpGet("{userId:long}/following")]
        public async Task<IActionResult> GetFollowing(long userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
        {
            var currentUserId = GetUserIdOptional();
            var result = await _userService.GetFollowingAsync(userId, currentUserId, pageNumber, pageSize, ct);
            return Ok(ApiResponse<PagedResult<UserSummaryDto>>.Ok(result, "Following retrieved successfully."));
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

        private long? GetUserIdOptional()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
}
