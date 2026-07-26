using System;

namespace REVORA_BE.DTOs.CreditPackage
{
    public class UserCreditBatchResponseDto
    {
        public long UserCreditBatchId { get; set; }
        public long CreditTypeId { get; set; }
        public string? CreditTypeName { get; set; }
        public int RemainingCredits { get; set; }
        public DateTime? ExpiresAt { get; set; }

        /// <summary>true = gói trả phí, false = gói free (quà / event).</summary>
        public bool IsPaid { get; set; }

        /// <summary>Id gói catalog (PaidCreditPackageId hoặc FreeCreditPackageId).</summary>
        public int PackageId { get; set; }

        public string? PackageName { get; set; }
    }
}