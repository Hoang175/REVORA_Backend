using System;

namespace REVORA_BE.DTOs.Response
{
    public class ShortResponseDto
    {
        public long ShortId { get; set; }
        public string VideoUrl { get; set; } = null!;
        public string? Caption { get; set; }
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin người bán
        public long SellerId { get; set; }
        public string SellerName { get; set; } = null!;
        public string SellerAvatar { get; set; } = null!;
        public string? SellerBadgeName { get; set; }

        // Liên kết sản phẩm (nếu có)
        public long? ProductId { get; set; }
        public decimal? ProductPrice { get; set; }
        public string? ProductTitle { get; set; }

        // Trạng thái user hiện tại
        public bool IsLikedByCurrentUser { get; set; }
        public bool IsFollowingSeller { get; set; }
    }

    public class ShortCommentResponseDto
    {
        public long CommentId { get; set; }
        public long UserId { get; set; }
        public long? ParentId { get; set; }
        public string Username { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string AvatarUrl { get; set; } = null!;
        public string? UserBadgeName { get; set; }
        public string Content { get; set; } = null!;
        public int LikeCount { get; set; }
        public bool IsLikedByCurrentUser { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}