using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IMatchTradeService
    {
        Task<MatchCommunityStatsDto> GetCommunityStatsAsync();
        Task BroadcastMatchStatsAsync();
        Task<List<MatchOfferingProductDto>> GetMyOfferingProductsAsync(long userId);
        Task<MatchFilterOptionsDto> GetFilterOptionsAsync(long userId);
        Task<MatchFilterPreviewDto> PreviewFiltersAsync(long userId, PreviewMatchFiltersRequestDto request);
        Task<MatchSessionResponseDto> StartSessionAsync(long userId, StartMatchSessionRequestDto request);
        Task<MatchSessionResponseDto?> GetActiveSessionAsync(long userId);
        Task<MatchSwipeResultDto> GetNextCardAsync(long userId, long sessionId);
        Task<MatchSwipeResultDto> SwipeAsync(long userId, long sessionId, MatchSwipeRequestDto request);
        Task UnlikeProductAsync(long userId, long sessionId, long targetProductId);
        Task<List<MatchOfferingProductDto>> GetTargetOfferingProductsAsync(long targetUserId);
        Task<MatchSwipeResultDto> BulkSwipeAsync(long userId, MatchBulkSwipeRequestDto request);
        Task EndSessionAsync(long userId, long sessionId);
        Task EndActiveSessionAsync(long userId);
        Task ExpireSessionAsync(long sessionId);
        Task<List<TradeMatchSummaryDto>> GetMyMatchesAsync(long userId, string? status = null);
        Task<TradeMatchSummaryDto?> GetMatchDetailAsync(long userId, long tradeMatchId);
        Task<TradeConfirmResultDto> ConfirmTradeAsync(long userId, long tradeMatchId);
        Task<TradeConfirmResultDto> DeclineConfirmAsync(long userId, long tradeMatchId);
        Task<TradeConfirmResultDto> LeaveTradeAsync(long userId, long tradeMatchId);
        Task<TradeConfirmResultDto> CancelMatchAsync(long userId, long tradeMatchId, bool isExpired);
        Task<TradeConfirmResultDto> FinishTradeAsync(long userId, long tradeMatchId);
        Task<TradeConfirmResultDto> NegotiateAsync(long userId, long tradeMatchId, MatchNegotiateRequestDto request);
        Task<List<MatchLikedProductDto>> GetMyLikedProductsAsync(long userId, long sessionId);
        Task<List<MatchInterestInboxItemDto>> GetInterestInboxAsync(long userId);
        Task<TradeMutualLikesDto> GetMutualLikesInTradeAsync(long userId, long tradeMatchId);
    }
}
