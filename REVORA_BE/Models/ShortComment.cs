using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("ShortComments")]
    public class ShortComment
    {
        public long CommentId { get; set; }

        public long ShortId { get; set; }

        public long UserId { get; set; }

        public long? ParentId { get; set; }

        public string Content { get; set; } = null!;

        public int LikeCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public Short? Short { get; set; }

        public User? User { get; set; }

        public ShortComment? ParentComment { get; set; }

        public ICollection<ShortComment> ChildComments { get; set; } = new HashSet<ShortComment>();
    }
}
