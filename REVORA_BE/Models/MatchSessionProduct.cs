using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("MatchSessionProducts")]
    public class MatchSessionProduct
    {
        public long MatchSessionId { get; set; }

        public long ProductId { get; set; }

        public MatchSession? MatchSession { get; set; }

        public Product? Product { get; set; }
    }
}
