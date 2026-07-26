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
    public class MatchTradeRepository : IMatchTradeRepository
    {
        private readonly AppDbContext _context;

        public MatchTradeRepository(AppDbContext context)
        {
            _context = context;
        }

        private static IQueryable<Product> EligibleProductsQuery(AppDbContext ctx, long excludeUserId)
        {
            var now = DateTime.UtcNow;
            return ctx.MatchSessionProducts
                .Include(msp => msp.MatchSession)
                .Include(msp => msp.Product).ThenInclude(p => p!.Seller)
                .Include(msp => msp.Product).ThenInclude(p => p!.ProductImages)
                .Where(msp => msp.MatchSession != null && msp.MatchSession.Status == MatchSessionStatus.Active.ToString())
                .Where(msp => msp.Product != null
                    && msp.Product.SellerId != excludeUserId
                    && msp.Product.ProductStatus == "Public"
                    && (!msp.Product.ProductExpiredAt.HasValue || msp.Product.ProductExpiredAt > now))
                .Select(msp => msp.Product!);
        }

        private static IQueryable<Product> ApplyFilters(IQueryable<Product> query, decimal minPrice, decimal maxPrice, string? city)
        {
            query = query.Where(p => p.Price >= minPrice);
            if (maxPrice < decimal.MaxValue)
                query = query.Where(p => p.Price <= maxPrice);
            if (!string.IsNullOrWhiteSpace(city))
                query = query.Where(p => p.Seller != null && p.Seller.City == city);
            return query;
        }

        public async Task<List<Product>> GetUserOfferingProductsAsync(long userId)
        {
            var now = DateTime.UtcNow;
            return await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.SellerId == userId
                    && !p.IsMatchSeed
                    && p.ProductStatus == "Public"
                    && (!p.ProductExpiredAt.HasValue || p.ProductExpiredAt > now))
                .OrderByDescending(p => p.ProductCreateAt)
                .ToListAsync();
        }

        public async Task<MatchSession?> GetActiveSessionAsync(long userId) =>
            await _context.MatchSessions
                .Include(s => s.OfferingProducts).ThenInclude(op => op.Product).ThenInclude(p => p!.ProductImages)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == MatchSessionStatus.Active.ToString());

        public async Task<MatchSession?> GetSessionByIdAsync(long sessionId, long userId) =>
            await _context.MatchSessions
                .Include(s => s.OfferingProducts).ThenInclude(op => op.Product).ThenInclude(p => p!.ProductImages)
                .FirstOrDefaultAsync(s => s.MatchSessionId == sessionId && s.UserId == userId);

        public async Task<List<long>> GetSwipedProductIdsAsync(long sessionId) =>
            await _context.MatchSwipes
                .Where(s => s.MatchSessionId == sessionId)
                .Select(s => s.TargetProductId)
                .ToListAsync();

        public async Task<int> CountEligibleProductsAsync(long userId, decimal minPrice, decimal maxPrice, string? city, List<long> excludeProductIds)
        {
            var query = ApplyFilters(EligibleProductsQuery(_context, userId), minPrice, maxPrice, city);
            if (excludeProductIds.Count > 0)
                query = query.Where(p => !excludeProductIds.Contains(p.ProductId));
            return await query.CountAsync();
        }

        public async Task<int> CountEligibleParticipantsAsync(long userId, decimal minPrice, decimal maxPrice, string? city)
        {
            var query = ApplyFilters(EligibleProductsQuery(_context, userId), minPrice, maxPrice, city);
            return await query.Select(p => p.SellerId).Distinct().CountAsync();
        }

        public async Task<Product?> GetNextSwipeProductAsync(long userId, long sessionId, decimal minPrice, decimal maxPrice, string? city, List<long> swipedIds)
        {
            var query = ApplyFilters(EligibleProductsQuery(_context, userId), minPrice, maxPrice, city);
            if (swipedIds.Count > 0)
                query = query.Where(p => !swipedIds.Contains(p.ProductId));

            var products = await query
                .OrderBy(p => p.IsMatchSeed)
                .ThenByDescending(p => p.ProductCreateAt)
                .Take(1)
                .ToListAsync();

            return products.FirstOrDefault();
        }

        public async Task<MatchSession> CreateSessionAsync(MatchSession session, List<long> productIds)
        {
            var active = await GetActiveSessionAsync(session.UserId);
            if (active != null)
            {
                await CleanupSessionTempDataAsync(active.MatchSessionId, session.UserId);
                active.Status = MatchSessionStatus.Ended.ToString();
                active.EndedAt = DateTime.UtcNow;
            }

            _context.MatchSessions.Add(session);
            await _context.SaveChangesAsync();

            foreach (var productId in productIds)
            {
                _context.MatchSessionProducts.Add(new MatchSessionProduct
                {
                    MatchSessionId = session.MatchSessionId,
                    ProductId = productId
                });
            }

            await _context.SaveChangesAsync();
            return session;
        }

        public async Task EndSessionAsync(long sessionId, string? finalStatus = null)
        {
            var session = await _context.MatchSessions.FindAsync(sessionId);
            if (session == null) return;
            session.Status = finalStatus ?? MatchSessionStatus.Ended.ToString();
            session.EndedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task AddSwipeAsync(MatchSwipe swipe)
        {
            _context.MatchSwipes.Add(swipe);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasSwipedAsync(long sessionId, long productId) =>
            await _context.MatchSwipes.AnyAsync(s => s.MatchSessionId == sessionId && s.TargetProductId == productId);

        public async Task AddInterestNotificationAsync(MatchInterestNotification notification)
        {
            _context.MatchInterestNotifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task<MatchSwipe?> GetMatchSwipeAsync(long userId, long sessionId, long targetProductId)
        {
            return await _context.MatchSwipes
                .Include(s => s.TargetProduct)
                .FirstOrDefaultAsync(s => s.UserId == userId && s.MatchSessionId == sessionId && s.TargetProductId == targetProductId && s.Direction == "Like");
        }

        public async Task RemoveSwipeAsync(MatchSwipe swipe)
        {
            _context.MatchSwipes.Remove(swipe);
            await _context.SaveChangesAsync();
        }

        public async Task<List<long>> RemoveInterestNotificationAsync(long interestedUserId, long likedProductId)
        {
            var notifs = await _context.MatchInterestNotifications
                .Where(n => n.InterestedUserId == interestedUserId && n.LikedProductId == likedProductId)
                .ToListAsync();

            var ids = notifs.Select(n => n.MatchInterestNotificationId).ToList();
            _context.MatchInterestNotifications.RemoveRange(notifs);
            await _context.SaveChangesAsync();
            return ids;
        }

        public async Task<bool> HasActiveMatchWithUserAsync(long userId, long partnerId)
        {
            return await _context.TradeMatches.AnyAsync(t =>
                ((t.UserLowId == userId && t.UserHighId == partnerId) || (t.UserHighId == userId && t.UserLowId == partnerId))
                && t.Status == TradeMatchStatus.Active.ToString());
        }

        public async Task<MatchSwipe?> FindMutualLikeAsync(long swiperUserId, long targetProductOwnerId, long swiperSessionId)
        {
            return await _context.MatchSwipes
                .Include(s => s.MatchSession)
                .Include(s => s.TargetProduct)
                .Where(s => s.UserId == targetProductOwnerId
                    && s.Direction == MatchSwipeDirection.Like.ToString()
                    && s.MatchSession != null
                    && s.MatchSession.Status == MatchSessionStatus.Active.ToString()
                    && s.TargetProduct != null
                    && s.TargetProduct.SellerId == swiperUserId)
                .OrderByDescending(s => s.SwipedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<TradeMatch> CreateTradeMatchAsync(TradeMatch match)
        {
            _context.TradeMatches.Add(match);
            await _context.SaveChangesAsync();
            return match;
        }

        public async Task<TradeMatch?> GetTradeMatchAsync(long tradeMatchId, long userId) =>
            await _context.TradeMatches
                .Include(t => t.ProductLowUser).ThenInclude(p => p!.ProductImages)
                .Include(t => t.ProductHighUser).ThenInclude(p => p!.ProductImages)
                .Include(t => t.UserLow)
                .Include(t => t.UserHigh)
                .FirstOrDefaultAsync(t => t.TradeMatchId == tradeMatchId
                    && (t.UserLowId == userId || t.UserHighId == userId));

        public async Task<List<TradeMatch>> GetUserTradeMatchesAsync(long userId, string? status = null)
        {
            var query = _context.TradeMatches
                .Include(t => t.ProductLowUser).ThenInclude(p => p!.ProductImages)
                .Include(t => t.ProductHighUser).ThenInclude(p => p!.ProductImages)
                .Include(t => t.UserLow)
                .Include(t => t.UserHigh)
                .Where(t => t.UserLowId == userId || t.UserHighId == userId);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(t => t.Status == status);

            return await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        }

        public async Task CleanupSessionTempDataAsync(long sessionId, long userId, long? partnerUserId = null)
        {
            var swipesQuery = _context.MatchSwipes.Where(s => s.MatchSessionId == sessionId);
            
            if (partnerUserId.HasValue)
            {
                // Giữ lại lịch sử Tym giữa userId và partnerUserId để dùng cho hiển thị danh sách mutual-likes
                swipesQuery = swipesQuery.Where(s => 
                    !(s.UserId == userId && s.TargetProduct != null && s.TargetProduct.SellerId == partnerUserId) &&
                    !(s.UserId == partnerUserId && s.TargetProduct != null && s.TargetProduct.SellerId == userId)
                );
            }

            var swipes = await swipesQuery.ToListAsync();
            _context.MatchSwipes.RemoveRange(swipes);

            var notifQuery = _context.MatchInterestNotifications
                .Where(n => n.MatchSessionId == sessionId
                    || n.InterestedUserId == userId
                    || n.OwnerUserId == userId);

            if (partnerUserId.HasValue)
            {
                notifQuery = _context.MatchInterestNotifications.Where(n =>
                    n.MatchSessionId == sessionId
                    || n.InterestedUserId == userId
                    || n.OwnerUserId == userId
                    || n.InterestedUserId == partnerUserId
                    || n.OwnerUserId == partnerUserId);
            }

            var notifications = await notifQuery.ToListAsync();
            _context.MatchInterestNotifications.RemoveRange(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTradeMatchAsync(TradeMatch match)
        {
            _context.TradeMatches.Update(match);
            await _context.SaveChangesAsync();
        }

        public async Task IncrementTradeSuccessAsync(long userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.TradeSuccessCount++;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Product>> GetEligibleProductsForStatsAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.MatchSessionProducts
                .Include(msp => msp.MatchSession)
                .Include(msp => msp.Product)
                .Where(msp => msp.MatchSession != null && msp.MatchSession.Status == MatchSessionStatus.Active.ToString())
                .Where(msp => msp.Product != null
                    && msp.Product.ProductStatus == "Public"
                    && (!msp.Product.ProductExpiredAt.HasValue || msp.Product.ProductExpiredAt > now))
                .Select(msp => msp.Product!)
                .Distinct()
                .ToListAsync();
        }

        public async Task<int> CountActiveSessionUsersAsync() =>
            await _context.MatchSessions
                .Where(s => s.Status == MatchSessionStatus.Active.ToString())
                .Select(s => s.UserId)
                .Distinct()
                .CountAsync();
    }
}
