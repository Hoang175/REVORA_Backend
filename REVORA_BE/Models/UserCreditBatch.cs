using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace REVORA_BE.Models
{
    [Table("UserCreditBatches")]
    public class UserCreditBatch
    {
        public long BatchId { get; set; }

        public long UserId { get; set; }

        public int CreditTypeId { get; set; }

        public int? PaidPackageId { get; set; }

        public int? FreePackageId { get; set; }

        public long? OrderId { get; set; }

        public int RemainingCredits { get; set; }

        public DateTime? ClaimedAt { get; set; }

        public DateTime? PurchasedAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; }

        public User? User { get; set; }

        public CreditType? CreditType { get; set; }

        public PaidCreditPackage? PaidCreditPackage { get; set; }

        public FreeCreditPackage? FreeCreditPackage { get; set; }

        public Order? Order { get; set; }
    }
}
