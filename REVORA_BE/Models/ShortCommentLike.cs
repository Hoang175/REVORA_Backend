using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("ShortCommentLikes")]
    public class ShortCommentLike
    {
        public long CommentId { get; set; }
        public long UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public ShortComment? ShortComment { get; set; }
        public User? User { get; set; }
    }
}
