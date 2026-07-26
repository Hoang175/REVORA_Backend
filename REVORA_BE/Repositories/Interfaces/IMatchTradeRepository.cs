using REVORA_BE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Interfaces
{
    public interface IMatchTradeRepository
    {
        Task<List<Product>> GetUserOfferingProductsAsync(long userId);
        Task<MatchSession?> GetActiveSessionAsync(long userId);
        Task<MatchSession?> GetSessionByIdAsync(long sessionId, long userId);
        Task<List<long>> GetSwipedProductIdsAsync(long sessionId);
        Task<int> CountEligibleProductsAsync(long userId, decimal minPrice, decimal maxPrice, string? city, List<long> excludeProductIds);
        Task<int> CountEligibleParticipantsAsync(long userId, decimal minPrice, decimal maxPrice, string? city);
        Task<Product?> GetNextSwipeProductAsync(long userId, long sessionId, decimal minPrice, decimal maxPrice, string? city, List<long> swipedIds);
        Task<MatchSession> CreateSessionAsync(MatchSession session, List<long> productIds);
        Task EndSessionAsync(long sessionId, string? finalStatus = null);
        Task AddSwipeAsync(MatchSwipe swipe);
        Task<bool> HasSwipedAsync(long sessionId, long productId);
        Task AddInterestNotificationAsync(MatchInterestNotification notification);
        Task<MatchSwipe?> GetMatchSwipeAsync(long userId, long sessionId, long targetProductId);
        Task RemoveSwipeAsync(MatchSwipe swipe);
        Task<List<long>> RemoveInterestNotificationAsync(long interestedUserId, long likedProductId);
        Task<bool> HasActiveMatchWithUserAsync(long userId, long partnerId);
        Task<MatchSwipe?> FindMutualLikeAsync(long swiperUserId, long targetProductOwnerId, long swiperSessionId);
        Task<TradeMatch> CreateTradeMatchAsync(TradeMatch match);
        Task<TradeMatch?> GetTradeMatchAsync(long tradeMatchId, long userId);
        Task<List<TradeMatch>> GetUserTradeMatchesAsync(long userId, string? status = null);
        Task CleanupSessionTempDataAsync(long sessionId, long userId, long? partnerUserId = null);
        Task UpdateTradeMatchAsync(TradeMatch match);
        Task IncrementTradeSuccessAsync(long userId);
        Task<List<Product>> GetEligibleProductsForStatsAsync();
        Task<int> CountActiveSessionUsersAsync();
    }
}
