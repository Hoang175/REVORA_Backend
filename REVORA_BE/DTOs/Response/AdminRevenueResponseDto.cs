using System.Collections.Generic;

namespace REVORA_BE.DTOs.Response
{
    public class AdminRevenueResponseDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }
        public List<RevenueByPackageDto> RevenueByPackages { get; set; } = new List<RevenueByPackageDto>();
        public List<AdminRevenueChartItemDto> ChartData { get; set; } = new List<AdminRevenueChartItemDto>();
        public List<AdminTransactionDto> Transactions { get; set; } = new List<AdminTransactionDto>();
    }

    public class RevenueByPackageDto
    {
        public string PackageName { get; set; } = null!;
        public decimal Revenue { get; set; }
    }

    public class AdminRevenueChartItemDto
    {
        public string Label { get; set; } = null!;
        public decimal Posting { get; set; }
        public decimal Featured { get; set; }
    }

    public class AdminTransactionDto
    {
        public string Id { get; set; } = null!;
        public string User { get; set; } = null!;
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string Package { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Date { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
