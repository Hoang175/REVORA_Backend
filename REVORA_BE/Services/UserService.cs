using Microsoft.EntityFrameworkCore;
using REVORA_BE.DTOs;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Exceptions;
using REVORA_BE.Models;
using REVORA_BE.Services.Interfaces;

namespace REVORA_BE.Services
{
    public interface IUserService
    {
        Task<UserProfileDto> GetMyProfileAsync(long userId, CancellationToken ct);
        Task<UserProfileDto> GetUserProfileAsync(long userId, long? currentUserId, CancellationToken ct);
        Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileDto dto, CancellationToken ct);
        Task<bool> ToggleFollowAsync(long currentUserId, long targetUserId, CancellationToken ct);
        Task<PagedResult<UserSummaryDto>> GetFollowersAsync(long userId, long? currentUserId, int pageNumber, int pageSize, CancellationToken ct);
        Task<PagedResult<UserSummaryDto>> GetFollowingAsync(long userId, long? currentUserId, int pageNumber, int pageSize, CancellationToken ct);
        Task<UserProfileDto> UpdateBadgeAsync(long userId, int? badgeId, CancellationToken ct);
        Task<List<REVORA_BE.DTOs.Response.BadgeResponseDto>> GetBadgesAsync(long userId, CancellationToken ct);
    }

    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public UserService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<UserProfileDto> GetMyProfileAsync(long userId, CancellationToken ct)
        {
            return await GetUserProfileAsync(userId, userId, ct);
        }

        public async Task<UserProfileDto> GetUserProfileAsync(long userId, long? currentUserId, CancellationToken ct)
        {
            var user = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user == null)
                throw new NotFoundException(
                    clientMessage: "User not found",
                    internalMessage: "User with the specified ID does not exist.",
                    code: "UserNotFound"
                );

            var soldCount = await _context.Products
                .CountAsync(p => p.SellerId == userId && p.ProductStatus == "Sold", ct);

            var sellingCount = await _context.Products
                .CountAsync(p => p.SellerId == userId && p.ProductStatus == "Public" && p.ProductExpiredAt > DateTime.UtcNow, ct);

            var followerCount = await _context.UserFollows
                .CountAsync(uf => uf.FolloweeId == userId, ct);

            var followingCount = await _context.UserFollows
                .CountAsync(uf => uf.FollowerId == userId, ct);

            bool isFollowing = false;
            if (currentUserId.HasValue && currentUserId.Value != userId)
            {
                isFollowing = await _context.UserFollows
                    .AnyAsync(uf => uf.FollowerId == currentUserId.Value && uf.FolloweeId == userId, ct);
            }

            REVORA_BE.DTOs.Response.BadgeResponseDto? badgeDto = null;
            if (user.BadgeId.HasValue)
            {
                var userBadge = await _context.UserBadges.FirstOrDefaultAsync(ub => ub.UserId == userId && ub.BadgeId == user.BadgeId.Value, ct);
                if (userBadge != null && (!userBadge.ExpiredAt.HasValue || userBadge.ExpiredAt.Value > DateTime.UtcNow))
                {
                    var badge = await _context.Badges
                        .AsNoTracking()
                        .FirstOrDefaultAsync(b => b.BadgeId == user.BadgeId.Value, ct);
                    if (badge != null)
                    {
                        badgeDto = new REVORA_BE.DTOs.Response.BadgeResponseDto
                        {
                            BadgeId = badge.BadgeId,
                            Name = badge.Name,
                            IconUrl = badge.IconUrl,
                            Description = badge.Description,
                            IsOwned = true,
                            ExpiredAt = userBadge.ExpiredAt
                        };
                    }
                }
                else
                {
                    var trackedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
                    if (trackedUser != null)
                    {
                        trackedUser.BadgeId = null;
                        await _context.SaveChangesAsync(ct);
                    }
                }
            }

            return new UserProfileDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Bio = user.Bio,
                Birthday = user.Birthday,
                Gender = user.Gender,
                Address = user.Address,
                City = user.City,
                CreatedAt = user.CreatedAt,
                SoldCount = soldCount,
                SellingCount = sellingCount,
                FollowerCount = followerCount,
                FollowingCount = followingCount,
                IsFollowing = isFollowing,
                BadgeId = user.BadgeId,
                Badge = badgeDto
            };
        }

        public async Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileDto dto, CancellationToken ct)
        {
            var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user == null)
                throw new NotFoundException(
                    clientMessage: "User not found",
                    internalMessage: "User with the specified ID does not exist.",
                    code: "UserNotFound"
                );

            user.FullName = dto.FullName.Trim();
            user.Phone = dto.Phone?.Trim();
            user.Birthday = dto.Birthday;
            user.Gender = dto.Gender?.Trim();
            user.Address = dto.Address?.Trim();
            user.City = dto.City?.Trim();
            user.Bio = dto.Bio?.Trim();
            
            if (!string.IsNullOrEmpty(dto.AvatarUrl))
            {
                user.AvatarUrl = dto.AvatarUrl;
            }

            await _context.SaveChangesAsync(ct);

            return await GetUserProfileAsync(userId, userId, ct);
        }

        public async Task<bool> ToggleFollowAsync(long currentUserId, long targetUserId, CancellationToken ct)
        {
            if (currentUserId == targetUserId)
                throw new ValidationException("You cannot follow yourself.");

            var targetUserExists = await _context.Users.AnyAsync(u => u.UserId == targetUserId, ct);
            if (!targetUserExists)
                throw new NotFoundException("User not found", "Target user does not exist", "UserNotFound");

            var existingFollow = await _context.UserFollows
                .FirstOrDefaultAsync(uf => uf.FollowerId == currentUserId && uf.FolloweeId == targetUserId, ct);

            if (existingFollow != null)
            {
                // Unfollow
                _context.UserFollows.Remove(existingFollow);
                await _context.SaveChangesAsync(ct);
                return false; // Result is "not following"
            }
            else
            {
                // Follow
                _context.UserFollows.Add(new UserFollow
                {
                    FollowerId = currentUserId,
                    FolloweeId = targetUserId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync(ct);

                var follower = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUserId, ct);
                if (follower != null)
                {
                    await _notificationService.CreateNotificationAsync(
                        userId: targetUserId,
                        type: "follow",
                        title: "Người theo dõi mới",
                        message: $"{follower.FullName ?? follower.Username} đã bắt đầu theo dõi bạn.",
                        referenceId: currentUserId.ToString()
                    );
                }

                return true; // Result is "following"
            }
        }

        public async Task<PagedResult<UserSummaryDto>> GetFollowersAsync(long userId, long? currentUserId, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _context.UserFollows
                .Where(uf => uf.FolloweeId == userId);

            var totalRecords = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var follows = await query
                .Include(uf => uf.Follower)
                .OrderByDescending(uf => uf.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var users = follows.Select(uf => uf.Follower!).Where(u => u != null).ToList();

            var userIds = users.Select(u => u.UserId).ToList();
            var followerCounts = await _context.UserFollows
                .Where(uf => userIds.Contains(uf.FolloweeId))
                .GroupBy(uf => uf.FolloweeId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

            var followingSet = new HashSet<long>();
            if (currentUserId.HasValue)
            {
                var followingList = await _context.UserFollows
                    .Where(uf => uf.FollowerId == currentUserId.Value && userIds.Contains(uf.FolloweeId))
                    .Select(uf => uf.FolloweeId)
                    .ToListAsync(ct);
                followingSet = new HashSet<long>(followingList);
            }

            var items = users.Select(u => new UserSummaryDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                FollowerCount = followerCounts.TryGetValue(u.UserId, out var count) ? count : 0,
                IsFollowing = followingSet.Contains(u.UserId)
            }).ToList();

            return new PagedResult<UserSummaryDto>
            {
                Items = items,
                TotalCount = totalRecords,
                CurrentPage = pageNumber,
                TotalPages = totalPages
            };
        }

        public async Task<PagedResult<UserSummaryDto>> GetFollowingAsync(long userId, long? currentUserId, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _context.UserFollows
                .Where(uf => uf.FollowerId == userId);

            var totalRecords = await query.CountAsync(ct);
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var follows = await query
                .Include(uf => uf.Followee)
                .OrderByDescending(uf => uf.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var users = follows.Select(uf => uf.Followee!).Where(u => u != null).ToList();

            var userIds = users.Select(u => u.UserId).ToList();
            var followerCounts = await _context.UserFollows
                .Where(uf => userIds.Contains(uf.FolloweeId))
                .GroupBy(uf => uf.FolloweeId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

            var followingSet = new HashSet<long>();
            if (currentUserId.HasValue)
            {
                var followingList = await _context.UserFollows
                    .Where(uf => uf.FollowerId == currentUserId.Value && userIds.Contains(uf.FolloweeId))
                    .Select(uf => uf.FolloweeId)
                    .ToListAsync(ct);
                followingSet = new HashSet<long>(followingList);
            }

            var items = users.Select(u => new UserSummaryDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                Bio = u.Bio,
                FollowerCount = followerCounts.TryGetValue(u.UserId, out var count) ? count : 0,
                IsFollowing = followingSet.Contains(u.UserId)
            }).ToList();

            return new PagedResult<UserSummaryDto>
            {
                Items = items,
                TotalCount = totalRecords,
                CurrentPage = pageNumber,
                TotalPages = totalPages
            };
        }

        public async Task<UserProfileDto> UpdateBadgeAsync(long userId, int? badgeId, CancellationToken ct)
        {
            var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.UserId == userId, ct);
            if (user == null)
                throw new NotFoundException(
                    clientMessage: "User not found",
                    internalMessage: "User with the specified ID does not exist.",
                    code: "UserNotFound"
                );

            if (badgeId.HasValue)
            {
                // Verify the badge exists in the database
                var badgeExists = await _context.Badges.AnyAsync(b => b.BadgeId == badgeId.Value, ct);
                if (!badgeExists)
                    throw new NotFoundException(
                        clientMessage: "Badge not found",
                        internalMessage: "The specified badge does not exist.",
                        code: "BadgeNotFound"
                    );
            }

            user.BadgeId = badgeId;
            await _context.SaveChangesAsync(ct);

            return await GetUserProfileAsync(userId, userId, ct);
        }

        public async Task<List<REVORA_BE.DTOs.Response.BadgeResponseDto>> GetBadgesAsync(long userId, CancellationToken ct)
        {
            var userBadges = await _context.UserBadges
                .Where(ub => ub.UserId == userId)
                .ToDictionaryAsync(ub => ub.BadgeId, ub => ub.ExpiredAt, ct);

            var allBadges = await _context.Badges.AsNoTracking().ToListAsync(ct);
            var result = new List<REVORA_BE.DTOs.Response.BadgeResponseDto>();

            foreach (var b in allBadges)
            {
                bool hasRecord = userBadges.TryGetValue(b.BadgeId, out var expiredAt);
                bool isOwned = hasRecord && (!expiredAt.HasValue || expiredAt.Value > DateTime.UtcNow);

                result.Add(new REVORA_BE.DTOs.Response.BadgeResponseDto
                {
                    BadgeId = b.BadgeId,
                    Name = b.Name,
                    IconUrl = b.IconUrl,
                    Description = b.Description,
                    IsOwned = isOwned,
                    ExpiredAt = hasRecord ? expiredAt : null
                });
            }

            return result;
        }
    }
}