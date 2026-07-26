using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Shorts")]
    public class Short
    {
        public long ShortId { get; set; }

        public long SellerId { get; set; }

        public long? ProductId { get; set; }

        public string VideoUrl { get; set; } = null!;

        public string? Caption { get; set; }

        public int LikeCount { get; set; }

        public int CommentCount { get; set; }

        public string ShortStatus { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime? ExpiredAt { get; set; }

        public User? Seller { get; set; }

        public Product? Product { get; set; }

        public ICollection<ShortLike> ShortLikes { get; set; } = new HashSet<ShortLike>();

        public ICollection<ShortComment> ShortComments { get; set; } = new HashSet<ShortComment>();
    }
}
