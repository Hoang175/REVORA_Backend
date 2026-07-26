using System.Collections.Generic;

namespace REVORA_BE.DTOs.Response
{
    public class AdminDashboardResponseDto
    {
        public decimal CurrentMonthRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }
        
        public int PackagesSold { get; set; }
        public decimal PackagesSoldGrowth { get; set; }
        
        public int TotalUsers { get; set; }
        public decimal TotalUsersGrowth { get; set; }
        
        public int ActiveProducts { get; set; }
        public decimal ActiveProductsGrowth { get; set; }

        public List<AdminRevenueChartItemDto> RevenueChart7Days { get; set; } = new List<AdminRevenueChartItemDto>();
        public List<AdminPackageDistributionDto> PackageDistribution { get; set; } = new List<AdminPackageDistributionDto>();
        public List<AdminRecentActivityDto> RecentActivities { get; set; } = new List<AdminRecentActivityDto>();
    }

    public class AdminPackageDistributionDto
    {
        public string Name { get; set; } = null!;
        public int Value { get; set; }
        public string Color { get; set; } = null!;
    }

    public class AdminRecentActivityDto
    {
        public string User { get; set; } = null!;
        public string Action { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Time { get; set; } = null!;
    }
}
