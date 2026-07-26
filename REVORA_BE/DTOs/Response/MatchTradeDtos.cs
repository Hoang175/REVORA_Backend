using System;
using System.Collections.Generic;

namespace REVORA_BE.DTOs.Response
{
    public class MatchCommunityStatsDto
    {
        public int ActiveParticipants { get; set; }
        public int ProductsWaitingTrade { get; set; }
    }

    public class MatchOfferingProductDto
    {
        public long ProductId { get; set; }
        public string Title { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string ProductStatus { get; set; } = null!;
    }

    public class MatchFilterBucketDto
    {
        public string Label { get; set; } = null!;
        public decimal MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int ProductCount { get; set; }
        public int ParticipantCount { get; set; }
    }

    public class MatchCityFilterDto
    {
        public string City { get; set; } = null!;
        public int ProductCount { get; set; }
        public int ParticipantCount { get; set; }
    }

    public class MatchFilterOptionsDto
    {
        public List<MatchFilterBucketDto> PriceBuckets { get; set; } = new();
        public List<MatchCityFilterDto> Cities { get; set; } = new();
    }

    public class MatchFilterPreviewDto
    {
        public int EstimatedProducts { get; set; }
        public int EstimatedParticipants { get; set; }
    }

    public class MatchSessionResponseDto
    {
        public long MatchSessionId { get; set; }
        public string Status { get; set; } = null!;
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public string? City { get; set; }
        public List<MatchOfferingProductDto> OfferingProducts { get; set; } = new();
        public int EstimatedProducts { get; set; }
        public int EstimatedParticipants { get; set; }
        public DateTime StartedAt { get; set; }
    }

    public class MatchSwipeCardDto
    {
        public long ProductId { get; set; }
        public string Title { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Condition { get; set; }
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }
        public long SellerId { get; set; }
        public string SellerName { get; set; } = null!;
        public string? SellerCity { get; set; }
        public string? SellerAvatar { get; set; }
        public bool IsPremium { get; set; }
        public bool HasBadge { get; set; }
        public string? SellerBadgeName { get; set; }
        public bool IsMatchSeed { get; set; }
    }

    public class MatchSwipeResultDto
    {
        public bool HasMore { get; set; }
        public MatchSwipeCardDto? NextProduct { get; set; }
        public bool IsMutualMatch { get; set; }
        public TradeMatchSummaryDto? NewMatch { get; set; }
        public string? Message { get; set; }
    }

    public class TradeMatchSummaryDto
    {
        public long TradeMatchId { get; set; }
        public long ConversationId { get; set; }
        public long PartnerUserId { get; set; }
        public string PartnerName { get; set; } = null!;
        public string? PartnerAvatar { get; set; }
        public string? PartnerBadgeName { get; set; }
        public List<MatchOfferingProductDto> MyProducts { get; set; } = new();
        public List<MatchOfferingProductDto> PartnerProducts { get; set; } = new();
        public string Status { get; set; } = null!;
        public bool MyConfirmed { get; set; }
        public bool PartnerConfirmed { get; set; }
        public bool MyNegotiateConfirmed { get; set; }
        public bool PartnerNegotiateConfirmed { get; set; }
        public List<long> MySelectedProductIds { get; set; } = new();
        public List<long> PartnerSelectedProductIds { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class TradeConfirmResultDto
    {
        public long TradeMatchId { get; set; }
        public string Status { get; set; } = null!;
        public bool MyConfirmed { get; set; }
        public bool PartnerConfirmed { get; set; }
        public bool IsCompleted { get; set; }
        public string Message { get; set; } = null!;
    }

    /// <summary>Sản phẩm tôi đã Tym (liked) trong phiên.</summary>
    public class MatchLikedProductDto
    {
        public long ProductId { get; set; }
        public string Title { get; set; } = null!;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public string SellerName { get; set; } = null!;
        public DateTime SwipedAt { get; set; }
    }

    /// <summary>Ai đó đã Tym sản phẩm của tôi — hộp thư đến quan tâm.</summary>
    public class MatchInterestInboxItemDto
    {
        public long NotificationId { get; set; }
        public long InterestedUserId { get; set; }
        public string InterestedUserName { get; set; } = null!;
        public string? InterestedUserAvatar { get; set; }
        public string? InterestedUserBadgeName { get; set; }
        public long LikedProductId { get; set; }
        public string LikedProductTitle { get; set; } = null!;
        public string? LikedProductImage { get; set; }
        public long OfferingProductId { get; set; }
        public string OfferingProductTitle { get; set; } = null!;
        public string? OfferingProductImage { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class TradeMutualLikesDto
    {
        public List<MatchOfferingProductDto> MyLikedProducts { get; set; } = new();
        public List<MatchOfferingProductDto> PartnerLikedProducts { get; set; } = new();
    }
}
