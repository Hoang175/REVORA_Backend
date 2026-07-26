using Microsoft.EntityFrameworkCore;
using REVORA_BE.Data;
using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using REVORA_BE.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Implementations
{
    public class ShortRepository : IShortRepository
    {
        private readonly AppDbContext _context;

        public ShortRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Short>> GetAllActiveShortsAsync()
        {
            return await _context.Shorts
                .Include(s => s.Seller)
                .Include(s => s.Product)
                .Include(s => s.ShortLikes)
                .Where(s => s.ShortStatus == ShortStatus.Active.ToString() && (s.Product == null || s.Product.ProductStatus == "Public"))
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ChangeShortStatusAsync(long shortId, long userId, string status)
        {
            var shortVideo = await _context.Shorts.FindAsync(shortId);
            if (shortVideo == null || shortVideo.SellerId != userId) return false;

            shortVideo.ShortStatus = status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleLikeShortAsync(long shortId, long userId)
        {
            var shortVideo = await _context.Shorts.FindAsync(shortId);
            if (shortVideo == null) return false;

            var existingLike = await _context.ShortLikes
                .FirstOrDefaultAsync(l => l.ShortId == shortId && l.UserId == userId);

            bool isLikedNow;
            if (existingLike != null)
            {
                _context.ShortLikes.Remove(existingLike);
                shortVideo.LikeCount = Math.Max(0, shortVideo.LikeCount - 1);
                isLikedNow = false;
            }
            else
            {
                _context.ShortLikes.Add(new ShortLike { ShortId = shortId, UserId = userId, CreatedAt = DateTime.UtcNow });
                shortVideo.LikeCount += 1;
                isLikedNow = true;
            }

            await _context.SaveChangesAsync();
            return isLikedNow;
        }

        public async Task<List<ShortComment>> GetCommentsByShortIdAsync(long shortId)
        {
            return await _context.ShortComments
                .Include(c => c.User)
                .Where(c => c.ShortId == shortId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<ShortComment> AddShortCommentAsync(ShortComment comment)
        {
            _context.ShortComments.Add(comment);

            var shortVideo = await _context.Shorts.FindAsync(comment.ShortId);
            if (shortVideo != null) shortVideo.CommentCount += 1;

            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<ShortComment?> GetCommentByIdAsync(long commentId)
        {
            return await _context.ShortComments.FindAsync(commentId);
        }

        public async Task<ShortComment> UpdateShortCommentAsync(ShortComment comment)
        {
            _context.ShortComments.Update(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task DeleteShortCommentAsync(long commentId)
        {
            var comment = await _context.ShortComments
                .Include(c => c.ChildComments)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null) return;

            // Cascade delete children
            await DeleteCommentRecursiveAsync(comment);
            await _context.SaveChangesAsync();
        }

        private async Task DeleteCommentRecursiveAsync(ShortComment comment)
        {
            var children = await _context.ShortComments
                .Include(c => c.ChildComments)
                .Where(c => c.ParentId == comment.CommentId)
                .ToListAsync();

            foreach (var child in children)
            {
                await DeleteCommentRecursiveAsync(child);
            }

            var shortVideo = await _context.Shorts.FindAsync(comment.ShortId);
            if (shortVideo != null) shortVideo.CommentCount = Math.Max(0, shortVideo.CommentCount - 1);

            _context.ShortComments.Remove(comment);
        }

        public async Task<bool> ToggleLikeCommentAsync(long commentId, long userId)
        {
            var comment = await _context.ShortComments.FindAsync(commentId);
            if (comment == null) return false;

            var existingLike = await _context.ShortCommentLikes
                .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);

            bool isLikedNow;
            if (existingLike != null)
            {
                _context.ShortCommentLikes.Remove(existingLike);
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                isLikedNow = false;
            }
            else
            {
                _context.ShortCommentLikes.Add(new ShortCommentLike { CommentId = commentId, UserId = userId, CreatedAt = DateTime.UtcNow });
                comment.LikeCount += 1;
                isLikedNow = true;
            }

            await _context.SaveChangesAsync();
            return isLikedNow;
        }

        public async Task<List<long>> GetLikedCommentIdsAsync(long shortId, long userId)
        {
            return await _context.ShortCommentLikes
                .Where(l => l.UserId == userId && l.ShortComment!.ShortId == shortId)
                .Select(l => l.CommentId)
                .ToListAsync();
        }

        public async Task<List<long>> GetFollowingSellerIdsAsync(long currentUserId)
        {
            return await _context.UserFollows
                .Where(f => f.FollowerId == currentUserId)
                .Select(f => f.FolloweeId)
                .ToListAsync();
        }
    }
}