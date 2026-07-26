using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Products")]
    public class Product
    {
        public long ProductId { get; set; }

        public long SellerId { get; set; }

        public int CategoryId { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public string? Brand { get; set; }

        public string? Condition { get; set; }

        public DateTime? ProductCreateAt { get; set; }

        public DateTime? ProductExpiredAt { get; set; }

        public string ProductStatus { get; set; } = null!;

        public DateTime? DeletedAt { get; set; }

        public int CommentCount { get; set; }

        public int ViewCount { get; set; }

        public bool IsUsedBanner { get; set; }

        public string? BannerUrl { get; set; }

        public DateTime? BannerExpiredAt { get; set; }

        public bool BannerStatus { get; set; }
        public bool HighlightStatus { get; set; }
        public DateTime? HighlightExpiredAt { get; set; }

        public bool IsUsedShort { get; set; }

        /// <summary>Sản phẩm mẫu cho Match — chỉ hiển thị, không tạo Match thật.</summary>
        public bool IsMatchSeed { get; set; }

        public User? Seller { get; set; }

        public Category? Category { get; set; }

        public ICollection<ProductImage> ProductImages { get; set; } = new HashSet<ProductImage>();

        public ICollection<Wishlist> Wishlists { get; set; } = new HashSet<Wishlist>();

        public ICollection<ProductComment> ProductComments { get; set; } = new HashSet<ProductComment>();

        public ICollection<Short> Shorts { get; set; } = new HashSet<Short>();
    }
}
