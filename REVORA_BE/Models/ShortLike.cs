using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("ShortLikes")]
    public class ShortLike
    {
        public long ShortId { get; set; }

        public long UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public Short? Short { get; set; }

        public User? User { get; set; }
    }
}
