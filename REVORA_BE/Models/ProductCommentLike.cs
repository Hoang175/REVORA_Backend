using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("ProductCommentLikes")]
    public class ProductCommentLike
    {
        public long CommentId { get; set; }

        public long UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public ProductComment? ProductComment { get; set; }

        public User? User { get; set; }
    }
}
