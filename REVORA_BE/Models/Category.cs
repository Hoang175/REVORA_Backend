using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("Categories")]
    public class Category
    {
        public int CategoryId { get; set; }

        public string Name { get; set; } = null!;

        public string IconUrl { get; set; } = null!;

        public bool IsActive { get; set; }

        public ICollection<Product> Products { get; set; } = new HashSet<Product>();
    }
}
