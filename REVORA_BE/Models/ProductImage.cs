using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("ProductImages")]
    public class ProductImage
    {
        public long ProductImageId { get; set; }

        public long ProductId { get; set; }

        public string ImageUrl { get; set; } = null!;

        public Product? Product { get; set; }
    }
}
