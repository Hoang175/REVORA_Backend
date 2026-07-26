using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Wishlists")]
    public class Wishlist
    {
        public long UserId { get; set; }

        public long ProductId { get; set; }

        public DateTime CreatedAt { get; set; }

        public User? User { get; set; }

        public Product? Product { get; set; }
    }
}
