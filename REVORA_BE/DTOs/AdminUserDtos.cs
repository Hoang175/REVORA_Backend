using System;

namespace REVORA_BE.DTOs
{
    public class AdminUserQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class AdminUserResponseDto
    {
        public long UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TradeSuccessCount { get; set; }
    }

    public class ToggleUserStatusDto
    {
        public bool IsActive { get; set; }
        public string Reason { get; set; } = null!;
    }

    public class TransactionResponseDto
    {
        public string OrderCode { get; set; } = null!;
        public string PackageName { get; set; } = null!;
        public string Type { get; set; } = null!;
        public int Credits { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class AdminUserOverviewDto
    {
        public int PostingCredits { get; set; }
        public int FeaturedCredits { get; set; }
        public decimal TotalSpent { get; set; }
        public int ProductsPosted { get; set; }
        public int TotalTransactions { get; set; }
        public List<TransactionResponseDto> RecentTransactions { get; set; } = new List<TransactionResponseDto>();
    }
}
