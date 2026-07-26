using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Models;
using REVORA_BE.Repositories.Interfaces;
using REVORA_BE.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace REVORA_BE.Services.Implementations
{
    public class ShortService : IShortService
    {
        private readonly IShortRepository _shortRepository;
        private readonly INotificationService _notificationService;
        private readonly AppDbContext _context;

        public ShortService(IShortRepository shortRepository, INotificationService notificationService, AppDbContext context)
        {
            _shortRepository = shortRepository;
            _notificationService = notificationService;
            _context = context;
        }

        public async Task<List<ShortResponseDto>> GetFeedShortsAsync(long? currentUserId)
        {
            var shorts = await _shortRepository.GetAllActiveShortsAsync();
            var followingIds = new List<long>();
            if (currentUserId.HasValue)
            {
                followingIds = await _shortRepository.GetFollowingSellerIdsAsync(currentUserId.Value);
            }

            var badgeMap = await _context.Badges.AsNoTracking().ToDictionaryAsync(b => b.BadgeId, b => b.Name);

            return shorts.Select(s => new ShortResponseDto
            {
                ShortId = s.ShortId,
                VideoUrl = s.VideoUrl,
                Caption = s.Caption ?? "Video thời trang cực chất",
                LikeCount = s.LikeCount,
                CommentCount = s.CommentCount,
                CreatedAt = s.CreatedAt,
                SellerId = s.SellerId,
                SellerName = s.Seller?.FullName ?? "Unknown",
                SellerAvatar = s.Seller?.AvatarUrl ?? "U",
                SellerBadgeName = (s.Seller != null && s.Seller.BadgeId.HasValue && badgeMap.TryGetValue(s.Seller.BadgeId.Value, out var badgeName)) ? badgeName : null,
                ProductId = s.ProductId,
                ProductPrice = s.Product?.Price,
                ProductTitle = s.Product?.Title,
                IsLikedByCurrentUser = currentUserId.HasValue && s.ShortLikes.Any(l => l.UserId == currentUserId.Value),
                IsFollowingSeller = followingIds.Contains(s.SellerId)
            }).ToList();
        }

        public async Task<bool> ToggleLikeAsync(long userId, long shortId)
        {
            var result = await _shortRepository.ToggleLikeShortAsync(shortId, userId);
            if (result)
            {
                var userWhoLiked = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                var shortVideo = await _context.Shorts.FirstOrDefaultAsync(s => s.ShortId == shortId);
                
                if (userWhoLiked != null && shortVideo != null && shortVideo.SellerId != userId)
                {
                    await _notificationService.CreateNotificationAsync(
                        userId: shortVideo.SellerId,
                        type: "like",
                        title: "Lượt thích mới",
                        message: $"{userWhoLiked.FullName ?? userWhoLiked.Username} đã thích video ngắn của bạn.",
                        referenceId: shortId.ToString()
                    );
                }
            }
            return result;
        }

        public async Task<List<ShortCommentResponseDto>> GetShortCommentsAsync(long shortId, long? currentUserId)
        {
            var comments = await _shortRepository.GetCommentsByShortIdAsync(shortId);
            
            var likedCommentIds = new List<long>();
            if (currentUserId.HasValue)
            {
                likedCommentIds = await _shortRepository.GetLikedCommentIdsAsync(shortId, currentUserId.Value);
            }

            var badgeMap = await _context.Badges.AsNoTracking().ToDictionaryAsync(b => b.BadgeId, b => b.Name);

            return comments.Select(c => new ShortCommentResponseDto
            {
                CommentId = c.CommentId,
                UserId = c.UserId,
                ParentId = c.ParentId,
                Username = c.User?.Username ?? "Unknown",
                FullName = c.User?.FullName ?? "User",
                AvatarUrl = c.User?.AvatarUrl ?? "U",
                UserBadgeName = (c.User != null && c.User.BadgeId.HasValue && badgeMap.TryGetValue(c.User.BadgeId.Value, out var badgeName)) ? badgeName : null,
                Content = c.Content,
                LikeCount = c.LikeCount,
                IsLikedByCurrentUser = likedCommentIds.Contains(c.CommentId),
                CreatedAt = c.CreatedAt
            }).ToList();
        }

        public async Task<ShortCommentResponseDto> AddCommentAsync(long userId, long shortId, CreateCommentRequestDto request)
        {
            var comment = new ShortComment
            {
                ShortId = shortId,
                UserId = userId,
                ParentId = request.ParentId,
                Content = request.Content,
                LikeCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            var saved = await _shortRepository.AddShortCommentAsync(comment);

            var allComments = await _shortRepository.GetCommentsByShortIdAsync(shortId);
            var freshComment = allComments.First(c => c.CommentId == saved.CommentId);

            var shortVideo = await _context.Shorts.FirstOrDefaultAsync(s => s.ShortId == shortId);

            // Notify parent commenter if replying
            if (request.ParentId.HasValue)
            {
                var parentComment = allComments.FirstOrDefault(c => c.CommentId == request.ParentId.Value);
                if (parentComment != null && parentComment.UserId != userId)
                {
                    await _notificationService.CreateNotificationAsync(
                        userId: parentComment.UserId,
                        type: "comment",
                        title: "Phản hồi bình luận trên Shorts",
                        message: $"{freshComment.User?.FullName ?? freshComment.User?.Username} đã trả lời bình luận của bạn: \"{freshComment.Content}\"",
                        referenceId: shortId.ToString()
                    );
                }
            }

            if (shortVideo != null && shortVideo.SellerId != userId && (!request.ParentId.HasValue || allComments.FirstOrDefault(c => c.CommentId == request.ParentId.Value)?.UserId != shortVideo.SellerId))
            {
                await _notificationService.CreateNotificationAsync(
                    userId: shortVideo.SellerId,
                    type: "comment",
                    title: "Bình luận mới trên Shorts",
                    message: $"{freshComment.User?.FullName ?? freshComment.User?.Username} đã bình luận: \"{freshComment.Content}\"",
                    referenceId: shortId.ToString()
                );
            }

            var badgeMap = await _context.Badges.AsNoTracking().ToDictionaryAsync(b => b.BadgeId, b => b.Name);

            return new ShortCommentResponseDto
            {
                CommentId = freshComment.CommentId,
                UserId = freshComment.UserId,
                ParentId = freshComment.ParentId,
                Username = freshComment.User?.Username ?? "Unknown",
                FullName = freshComment.User?.FullName ?? "User",
                AvatarUrl = freshComment.User?.AvatarUrl ?? "U",
                UserBadgeName = (freshComment.User != null && freshComment.User.BadgeId.HasValue && badgeMap.TryGetValue(freshComment.User.BadgeId.Value, out var bn)) ? bn : null,
                Content = freshComment.Content,
                LikeCount = freshComment.LikeCount,
                IsLikedByCurrentUser = false,
                CreatedAt = freshComment.CreatedAt
            };
        }

        public async Task<ShortCommentResponseDto?> UpdateCommentAsync(long userId, long commentId, UpdateCommentRequestDto request)
        {
            var comment = await _shortRepository.GetCommentByIdAsync(commentId);
            if (comment == null || comment.UserId != userId) return null;

            comment.Content = request.Content;
            await _shortRepository.UpdateShortCommentAsync(comment);

            var allComments = await _shortRepository.GetCommentsByShortIdAsync(comment.ShortId);
            var freshComment = allComments.FirstOrDefault(c => c.CommentId == commentId);
            if (freshComment == null) return null;

            var badgeMap = await _context.Badges.AsNoTracking().ToDictionaryAsync(b => b.BadgeId, b => b.Name);

            return new ShortCommentResponseDto
            {
                CommentId = freshComment.CommentId,
                UserId = freshComment.UserId,
                ParentId = freshComment.ParentId,
                Username = freshComment.User?.Username ?? "Unknown",
                FullName = freshComment.User?.FullName ?? "User",
                AvatarUrl = freshComment.User?.AvatarUrl ?? "U",
                UserBadgeName = (freshComment.User != null && freshComment.User.BadgeId.HasValue && badgeMap.TryGetValue(freshComment.User.BadgeId.Value, out var bn)) ? bn : null,
                Content = freshComment.Content,
                LikeCount = freshComment.LikeCount,
                IsLikedByCurrentUser = false, // Not fetched here, FE handles
                CreatedAt = freshComment.CreatedAt
            };
        }

        public async Task<bool> DeleteCommentAsync(long userId, long commentId)
        {
            var comment = await _shortRepository.GetCommentByIdAsync(commentId);
            if (comment == null || comment.UserId != userId) return false;

            await _shortRepository.DeleteShortCommentAsync(commentId);
            return true;
        }

        public async Task<bool> ToggleLikeCommentAsync(long userId, long commentId)
        {
            var isLiked = await _shortRepository.ToggleLikeCommentAsync(commentId, userId);
            if (isLiked)
            {
                var comment = await _shortRepository.GetCommentByIdAsync(commentId);
                if (comment != null && comment.UserId != userId)
                {
                    var user = await _context.Users.FindAsync(userId);
                    var userName = user?.FullName ?? user?.Username ?? "Một người dùng";
                    
                    await _notificationService.CreateNotificationAsync(
                        userId: comment.UserId,
                        type: "like",
                        title: "Lượt thích bình luận trên Shorts",
                        message: $"{userName} đã thích bình luận của bạn: \"{comment.Content}\"",
                        referenceId: comment.ShortId.ToString()
                    );
                }
            }
            return isLiked;
        }

        public async Task<bool> ChangeShortStatusAsync(long userId, long shortId, string status)
        {
            return await _shortRepository.ChangeShortStatusAsync(shortId, userId, status);
        }
    }
}