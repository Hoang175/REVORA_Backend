using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("ProductComments")]
    public class ProductComment
    {
        public long CommentId { get; set; }

        public long ProductId { get; set; }

        public long UserId { get; set; }

        public long? ParentId { get; set; }

        public string Content { get; set; } = null!;

        public int LikeCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public Product? Product { get; set; }

        public User? User { get; set; }

        public ProductComment? ParentComment { get; set; }

        public ICollection<ProductComment> ChildComments { get; set; } = new HashSet<ProductComment>();

        public ICollection<ProductCommentLike> CommentLikes { get; set; } = new HashSet<ProductCommentLike>();
    }
}
