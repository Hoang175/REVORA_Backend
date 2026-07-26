using System;

namespace REVORA_BE.DTOs.Response
{
    public class ProductResponseDto
    {
        public long ProductId { get; set; }
        public string Title { get; set; } = null!;
        public decimal Price { get; set; }
        public string? Condition { get; set; }
        public string Location { get; set; } = null!; 
        public string? ImageUrl { get; set; } 
        public string SellerName { get; set; } = null!;
        public string SellerFullName { get; set; } = null!;
        public bool IsPremium { get; set; } 
        public string? BannerUrl { get; set; }
        public int ViewCount { get; set; } 
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProductExpiredAt { get; set; }
        public DateTime? BannerExpiredAt { get; set; }
        public DateTime? ShortExpiredAt { get; set; }
        public DateTime? HighlightExpiredAt { get; set; }
        public string ProductStatus { get; set; } = "Public";
        public DateTime? DeletedAt { get; set; }
        public long? ShortId { get; set; }
        public string? ShortStatus { get; set; }
        public string? SellerBadgeName { get; set; }
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }


    public class ProductDetailResponseDto
    {
        public long ProductId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Brand { get; set; }
        public string Condition { get; set; }
        public string CategoryName { get; set; }
        public List<string> ImageUrls { get; set; }
        public string? VideoUrl { get; set; }
        public bool IsPremium { get; set; }
        public int ViewCount { get; set; }
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProductExpiredAt { get; set; }
        public DateTime? BannerExpiredAt { get; set; }
        public DateTime? ShortExpiredAt { get; set; }
        public DateTime? HighlightExpiredAt { get; set; }
        public string ProductStatus { get; set; }

        // Thông tin người bán
        public long SellerId { get; set; }
        public string SellerName { get; set; }
        public string SellerUsername { get; set; }
        public string SellerAvatar { get; set; }
        public string? SellerPhone { get; set; }
        public bool IsFollowingSeller { get; set; }
        public string? SellerBadgeName { get; set; }
    }
}