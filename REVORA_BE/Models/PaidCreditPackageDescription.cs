using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("PaidCreditPackageDescriptions")]
    public class PaidCreditPackageDescription
    {
        [Key]
        public int Id { get; set; }

        public int PaidCreditPackageId { get; set; }

        public string Content { get; set; } = null!;

        public int DisplayOrder { get; set; }

        public PaidCreditPackage? PaidCreditPackage { get; set; }
    }
}
