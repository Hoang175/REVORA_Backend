using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("CreditTypes")]
    public class CreditType
    {
        public int CreditTypeId { get; set; }

        public string Name { get; set; } = null!; 

        public ICollection<PaidCreditPackage> PaidCreditPackages { get; set; } = new HashSet<PaidCreditPackage>();

        public ICollection<FreeCreditPackage> FreeCreditPackages { get; set; } = new HashSet<FreeCreditPackage>();

        public ICollection<UserCreditBatch> UserCreditBatches { get; set; } = new HashSet<UserCreditBatch>();
    }
}
