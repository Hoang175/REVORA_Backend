using System;

namespace REVORA_BE.DTOs.Response
{
    public class CreditUsageLogResponseDto
    {
        public string Id { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Action { get; set; }
        public string CreditType { get; set; }
        public int Amount { get; set; }
        public string ProductName { get; set; }
        public string ProductId { get; set; }
        public int BalanceAfter { get; set; }
    }
}
