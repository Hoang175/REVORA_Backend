using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Users")]
    public class User
    {
        public long UserId { get; set; }

        public string Username { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string? Phone { get; set; }

        public string? AvatarUrl { get; set; }

        public string? Bio { get; set; }

        public DateTime? Birthday { get; set; }

        public string? Gender { get; set; }

        public string? Address { get; set; }

        public string? City { get; set; }

        public int RoleId { get; set; }

        public bool IsActive { get; set; }

        public bool IsOnline { get; set; }

        public int? BadgeId { get; set; }

        public bool IsFirstLogin { get; set; } // đăng ký gán = true, đăng nhập check true đổi thành false và cộng gói tân thủ (free) vào batch

        public DateTime CreatedAt { get; set; }

        public int TradeSuccessCount { get; set; }

        public Role Role { get; set; } = null!;

        public ICollection<RefreshToken> RefreshTokens { get; set; } = new HashSet<RefreshToken>();

        public ICollection<UserFollow> Followees { get; set; } = new HashSet<UserFollow>();

        public ICollection<UserFollow> Followers { get; set; } = new HashSet<UserFollow>();

        public ICollection<UserBadge> UserBadges { get; set; } = new HashSet<UserBadge>();

        public ICollection<Product> Products { get; set; } = new HashSet<Product>();

        public ICollection<Wishlist> Wishlists { get; set; } = new HashSet<Wishlist>();

        public ICollection<ProductComment> ProductComments { get; set; } = new HashSet<ProductComment>();

        public ICollection<ProductCommentLike> ProductCommentLikes { get; set; } = new HashSet<ProductCommentLike>();

        public ICollection<Short> Shorts { get; set; } = new HashSet<Short>();

        public ICollection<ShortLike> ShortLikes { get; set; } = new HashSet<ShortLike>();

        public ICollection<ShortComment> ShortComments { get; set; } = new HashSet<ShortComment>();

        public ICollection<UserCreditBatch> UserCreditBatches { get; set; } = new HashSet<UserCreditBatch>();

        public ICollection<Order> Orders { get; set; } = new HashSet<Order>();
    }
}
