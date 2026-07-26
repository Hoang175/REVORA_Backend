using Microsoft.EntityFrameworkCore;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using REVORA_BE.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using REVORA_BE.Hubs;
using REVORA_BE.DTOs.Request;
using Microsoft.Extensions.Logging;
using REVORA_BE.DTOs;
using REVORA_BE.Exceptions;
using REVORA_BE.Repositories.Interfaces;

namespace REVORA_BE.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<AdminService> _logger;
        private readonly IAdminAuditLogRepository _auditLogRepository;

        public AdminService(AppDbContext context, IHubContext<ChatHub> hubContext, ILogger<AdminService> logger, IAdminAuditLogRepository auditLogRepository)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<List<AdminProductResponseDto>> GetAllProductsAsync()
        {
            var products = await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Wishlists)
                .OrderByDescending(p => p.ProductCreateAt)
                .ToListAsync();

            return products.Select(p => {
                return new AdminProductResponseDto
                {
                    Id = p.ProductId.ToString(),
                    Title = p.Title ?? "",
                    Description = p.Description ?? "",
                    Price = p.Price,
                    Category = p.Category?.Name ?? "Khác",
                    Images = p.ProductImages.Select(x => x.ImageUrl).ToList(),
                    Owner = new AdminPostOwnerDto
                    {
                        Username = p.Seller?.Username ?? "Unknown",
                        Email = p.Seller?.Email ?? "unknown@example.com",
                        Avatar = p.Seller?.AvatarUrl != null ? p.Seller.AvatarUrl : (p.Seller?.Username != null ? p.Seller.Username.Substring(0, 2).ToUpper() : "U")
                    },
                    CreatedAt = p.ProductCreateAt?.ToString("dd/MM/yyyy") ?? DateTime.UtcNow.ToString("dd/MM/yyyy"),
                    Status = p.ProductStatus ?? "Public",
                    Views = p.Wishlists?.Count ?? 0,
                    ContactCount = p.CommentCount, // Dùng CommentCount tạm làm liên hệ
                    DeletedAt = p.DeletedAt,
                    IsFeatured = p.HighlightStatus && p.HighlightExpiredAt > DateTime.UtcNow,
                    Condition = p.Condition ?? "Khác",
                    Size = "M", // Size không có trong DB
                    Brand = p.Brand ?? "No Brand"
                };
            }).ToList();
        }

        public async Task<bool> UpdateProductStatusAsync(long productId, string status, string? note = null)
        {
            var product = await _context.Products.Include(p => p.Seller).FirstOrDefaultAsync(p => p.ProductId == productId);
            if (product == null) return false;

            var oldStatus = product.ProductStatus;
            product.ProductStatus = status;

            if (status == "Deleted" || status == "AdminDeleted")
            {
                product.DeletedAt = DateTime.UtcNow;
            }
            else if (status == "Public" || status == "Private")
            {
                product.DeletedAt = null;
            }

            if (status == "Violated" || status == "AdminDeleted" || (status == "Public" && (oldStatus == "AdminDeleted" || oldStatus == "AppealPending")))
            {
                string notificationTitle = "";
                string notificationMessage = "";
                string type = "info";

                if (status == "Violated" || status == "AdminDeleted")
                {
                    var reasonText = string.IsNullOrEmpty(note) ? "Không có lý do cụ thể" : note;
                    notificationTitle = status == "Violated" ? "⚠️ Cảnh báo vi phạm bài đăng" : "🗑️ Bài viết đã bị xóa";
                    notificationMessage = status == "Violated"
                        ? $"Bài đăng '{product.Title}' của bạn đã vi phạm chính sách với lý do: {reasonText}. Vui lòng liên hệ admin để biết thêm chi tiết."
                        : $"Bài đăng '{product.Title}' của bạn đã bị quản trị viên xóa với lý do: {reasonText}. Vui lòng liên hệ admin nếu có thắc mắc.";
                    type = status == "Violated" ? "warning" : "error";
                }
                else if (status == "Public" && oldStatus == "AdminDeleted")
                {
                    notificationTitle = "✅ Khôi phục bài đăng";
                    notificationMessage = $"Bài đăng '{product.Title}' của bạn đã được quản trị viên khôi phục thành công.";
                    type = "success";
                }
                else if (status == "Public" && oldStatus == "AppealPending")
                {
                    notificationTitle = "✅ Kháng cáo thành công";
                    notificationMessage = $"Kháng cáo cho bài đăng '{product.Title}' của bạn đã được quản trị viên chấp thuận. Bài đăng hiện đã được công khai trở lại.";
                    type = "success";
                }

                var notification = new Notification
                {
                    UserId = product.SellerId,
                    Type = type,
                    Title = notificationTitle,
                    Message = notificationMessage,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    IsSent = true,
                    ReferenceId = $"/product/{product.ProductId}"
                };

                await _context.Notifications.AddAsync(notification);

                // Try push SignalR
                if (ChatHub.UserConnections.TryGetValue(product.SellerId, out var connections))
                {
                    var payload = new NotificationResponseDto
                    {
                        Id = notification.NotificationId,
                        Type = notification.Type,
                        Title = notification.Title,
                        Message = notification.Message,
                        Time = "Vừa xong",
                        Read = false,
                        ReferenceId = notification.ReferenceId
                    };
                    _ = _hubContext.Clients.Clients(connections.ToList()).SendAsync("NewNotification", payload);
                }
            }
            
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AdminRevenueResponseDto> GetRevenueStatsAsync(string filterType, int? year, int? month, DateTime? customStartDate, DateTime? customEndDate)
        {
            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = DateTime.UtcNow;
            var now = DateTime.UtcNow;

            switch (filterType.ToLower())
            {
                case "year":
                    int targetYear = year ?? now.Year;
                    startDate = new DateTime(targetYear, 1, 1);
                    endDate = startDate.AddYears(1);
                    break;
                case "month":
                    int targetMonthYear = year ?? now.Year;
                    int targetMonth = month ?? now.Month;
                    startDate = new DateTime(targetMonthYear, targetMonth, 1);
                    endDate = startDate.AddMonths(1);
                    break;
                case "custom":
                    startDate = customStartDate ?? now.Date;
                    endDate = customEndDate?.AddDays(1) ?? now.Date.AddDays(1); // Include the end date fully
                    break;
                default:
                    startDate = new DateTime(now.Year, now.Month, 1);
                    endDate = startDate.AddMonths(1);
                    break;
            }

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaidCreditPackage)
                .ThenInclude(p => p.CreditType)
                .Where(o => o.PaymentStatus == PaymentStatus.Successful && o.PaidAt >= startDate && o.PaidAt < endDate)
                .OrderByDescending(o => o.PaidAt)
                .ToListAsync();

            var previousStartDate = startDate;
            var previousEndDate = startDate;
            switch (filterType.ToLower())
            {
                case "year": 
                    previousStartDate = startDate.AddYears(-1); 
                    previousEndDate = startDate;
                    break;
                case "month": 
                    previousStartDate = startDate.AddMonths(-1); 
                    previousEndDate = startDate;
                    break;
                case "custom": 
                    TimeSpan duration = endDate - startDate;
                    previousStartDate = startDate.Subtract(duration);
                    previousEndDate = startDate;
                    break;
                default: 
                    previousStartDate = startDate.AddMonths(-1); 
                    previousEndDate = startDate;
                    break;
            }

            var previousOrders = await _context.Orders
                .Where(o => o.PaymentStatus == PaymentStatus.Successful && o.PaidAt >= previousStartDate && o.PaidAt < previousEndDate)
                .ToListAsync();

            decimal totalRevenue = orders.Sum(o => o.AmountPaid);
            decimal previousRevenue = previousOrders.Sum(o => o.AmountPaid);
            decimal revenueGrowth = 0;
            if (previousRevenue > 0)
            {
                revenueGrowth = ((totalRevenue - previousRevenue) / previousRevenue) * 100;
            }
            else if (totalRevenue > 0)
            {
                revenueGrowth = 100;
            }

            var revenueByPackages = orders
                .Where(o => o.PaidCreditPackage != null && o.PaidCreditPackage.CreditType != null)
                .GroupBy(o => o.PaidCreditPackage!.CreditType!.Name)
                .Select(g => new RevenueByPackageDto
                {
                    PackageName = g.Key,
                    Revenue = g.Sum(o => o.AmountPaid)
                }).ToList();

            if (!revenueByPackages.Any(p => p.PackageName == "Posting")) revenueByPackages.Add(new RevenueByPackageDto { PackageName = "Posting", Revenue = 0 });
            if (!revenueByPackages.Any(p => p.PackageName == "Featured")) revenueByPackages.Add(new RevenueByPackageDto { PackageName = "Featured", Revenue = 0 });

            var chartData = new List<AdminRevenueChartItemDto>();
            TimeSpan currentDuration = endDate - startDate;

            if (filterType.ToLower() == "year")
            {
                for (int i = 1; i <= 12; i++)
                {
                    var monthStart = new DateTime(startDate.Year, i, 1);
                    var monthEnd = monthStart.AddMonths(1);
                    var monthOrders = orders.Where(o => o.PaidAt >= monthStart && o.PaidAt < monthEnd).ToList();
                    chartData.Add(new AdminRevenueChartItemDto
                    {
                        Label = $"T{i}",
                        Posting = monthOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Posting").Sum(o => o.AmountPaid),
                        Featured = monthOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Featured").Sum(o => o.AmountPaid)
                    });
                }
            }
            else if (filterType.ToLower() == "month" || currentDuration.TotalDays <= 31)
            {
                // Group by day
                int totalDays = (int)Math.Ceiling(currentDuration.TotalDays);
                for (int i = 0; i < totalDays; i++)
                {
                    var dayStart = startDate.AddDays(i);
                    var dayEnd = dayStart.AddDays(1);
                    var dayOrders = orders.Where(o => o.PaidAt >= dayStart && o.PaidAt < dayEnd).ToList();
                    
                    // Only show specific labels to avoid crowding, or just show all if < 15 days
                    string label = dayStart.ToString("dd/MM");
                    chartData.Add(new AdminRevenueChartItemDto
                    {
                        Label = label,
                        Posting = dayOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Posting").Sum(o => o.AmountPaid),
                        Featured = dayOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Featured").Sum(o => o.AmountPaid)
                    });
                }
            }
            else
            {
                // Group by month
                int totalMonths = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;
                if (totalMonths <= 0) totalMonths = 1;

                for (int i = 0; i < totalMonths; i++)
                {
                    var monthStart = startDate.AddMonths(i);
                    var monthEnd = monthStart.AddMonths(1);
                    if (monthEnd > endDate) monthEnd = endDate;
                    
                    var monthOrders = orders.Where(o => o.PaidAt >= monthStart && o.PaidAt < monthEnd).ToList();
                    chartData.Add(new AdminRevenueChartItemDto
                    {
                        Label = $"T{monthStart.Month}/{monthStart.Year}",
                        Posting = monthOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Posting").Sum(o => o.AmountPaid),
                        Featured = monthOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Featured").Sum(o => o.AmountPaid)
                    });
                }
            }

            var transactions = orders.Select(o => new AdminTransactionDto
            {
                Id = o.OrderCode,
                User = o.User?.Username ?? "Unknown",
                FullName = o.User?.FullName,
                AvatarUrl = o.User?.AvatarUrl,
                Package = o.PaidCreditPackage?.Name ?? "Unknown Package",
                Amount = o.AmountPaid,
                Date = ((o.PaidAt ?? o.CreatedAt).AddHours(7)).ToString("dd/MM/yyyy HH:mm"),
                Status = "Thành công"
            }).ToList();

            return new AdminRevenueResponseDto
            {
                TotalRevenue = totalRevenue,
                RevenueGrowth = Math.Round(revenueGrowth, 1),
                RevenueByPackages = revenueByPackages,
                ChartData = chartData,
                Transactions = transactions
            };
        }

        public async Task<AdminDashboardResponseDto> GetDashboardStatsAsync(string timeRange = "week")
        {
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var nextMonthStart = thisMonthStart.AddMonths(1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            // Doanh Thu Tháng Này
            var thisMonthOrders = await _context.Orders
                .Where(o => o.PaymentStatus == PaymentStatus.Successful && o.PaidAt >= thisMonthStart && o.PaidAt < nextMonthStart)
                .ToListAsync();
                
            var lastMonthOrders = await _context.Orders
                .Where(o => o.PaymentStatus == PaymentStatus.Successful && o.PaidAt >= lastMonthStart && o.PaidAt < thisMonthStart)
                .ToListAsync();

            decimal currentMonthRevenue = thisMonthOrders.Sum(o => o.AmountPaid);
            decimal lastMonthRevenue = lastMonthOrders.Sum(o => o.AmountPaid);
            decimal revenueGrowth = lastMonthRevenue > 0 
                ? ((currentMonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 
                : (currentMonthRevenue > 0 ? 100 : 0);

            // Số Gói Đã Bán
            int packagesSold = thisMonthOrders.Count;
            int lastMonthPackagesSold = lastMonthOrders.Count;
            decimal packagesSoldGrowth = lastMonthPackagesSold > 0
                ? ((decimal)(packagesSold - lastMonthPackagesSold) / lastMonthPackagesSold) * 100
                : (packagesSold > 0 ? 100 : 0);

            // Số Người Dùng
            int totalUsers = await _context.Users.CountAsync();
            var thisMonthUsers = await _context.Users.CountAsync(u => u.CreatedAt >= thisMonthStart && u.CreatedAt < nextMonthStart);
            var lastMonthUsers = await _context.Users.CountAsync(u => u.CreatedAt >= lastMonthStart && u.CreatedAt < thisMonthStart);
            decimal usersGrowth = lastMonthUsers > 0
                ? ((decimal)(thisMonthUsers - lastMonthUsers) / lastMonthUsers) * 100
                : (thisMonthUsers > 0 ? 100 : 0);

            // Số Sản Phẩm Đang Bán
            int activeProducts = await _context.Products.CountAsync(p => p.ProductStatus == "Public");
            int newProductsThisMonth = await _context.Products.CountAsync(p => p.ProductCreateAt >= thisMonthStart && p.ProductCreateAt < nextMonthStart);
            int newProductsLastMonth = await _context.Products.CountAsync(p => p.ProductCreateAt >= lastMonthStart && p.ProductCreateAt < thisMonthStart);
            decimal productsGrowth = newProductsLastMonth > 0
                ? ((decimal)(newProductsThisMonth - newProductsLastMonth) / newProductsLastMonth) * 100
                : (newProductsThisMonth > 0 ? 100 : 0);

            // Lấy danh sách giao dịch thành công theo khoảng thời gian được chọn
            DateTime filterStartDate;
            if (timeRange?.ToLower() == "month")
            {
                filterStartDate = now.Date.AddDays(-29);
            }
            else if (timeRange?.ToLower() == "year")
            {
                filterStartDate = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
            }
            else
            {
                filterStartDate = now.Date.AddDays(-6);
            }

            var timeRangeOrders = await _context.Orders
                .Include(o => o.PaidCreditPackage)
                .ThenInclude(p => p.CreditType)
                .Where(o => o.PaymentStatus == PaymentStatus.Successful && (o.PaidAt >= filterStartDate || (o.PaidAt == null && o.CreatedAt >= filterStartDate)))
                .ToListAsync();

            // Biểu Đồ Doanh Thu Theo Thời Gian
            var revenueChart = new List<AdminRevenueChartItemDto>();
            if (timeRange?.ToLower() == "month")
            {
                for (int i = 0; i < 30; i++)
                {
                    var dayStart = filterStartDate.AddDays(i);
                    var dayEnd = dayStart.AddDays(1);
                    var dayOrders = timeRangeOrders.Where(o => (o.PaidAt ?? o.CreatedAt) >= dayStart && (o.PaidAt ?? o.CreatedAt) < dayEnd).ToList();
                    revenueChart.Add(new AdminRevenueChartItemDto
                    {
                        Label = dayStart.ToString("dd/MM"),
                        Posting = dayOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Posting").Sum(o => o.AmountPaid),
                        Featured = dayOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Featured").Sum(o => o.AmountPaid)
                    });
                }
            }
            else if (timeRange?.ToLower() == "year")
            {
                for (int i = 0; i < 12; i++)
                {
                    var monthStart = filterStartDate.AddMonths(i);
                    var monthEnd = monthStart.AddMonths(1);
                    var monthOrders = timeRangeOrders.Where(o => (o.PaidAt ?? o.CreatedAt) >= monthStart && (o.PaidAt ?? o.CreatedAt) < monthEnd).ToList();
                    revenueChart.Add(new AdminRevenueChartItemDto
                    {
                        Label = $"T{monthStart.Month}/{monthStart.Year}",
                        Posting = monthOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Posting").Sum(o => o.AmountPaid),
                        Featured = monthOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Featured").Sum(o => o.AmountPaid)
                    });
                }
            }
            else
            {
                // default "week" (7 days)
                for (int i = 0; i < 7; i++)
                {
                    var dayStart = filterStartDate.AddDays(i);
                    var dayEnd = dayStart.AddDays(1);
                    var dayOrders = timeRangeOrders.Where(o => (o.PaidAt ?? o.CreatedAt) >= dayStart && (o.PaidAt ?? o.CreatedAt) < dayEnd).ToList();
                    revenueChart.Add(new AdminRevenueChartItemDto
                    {
                        Label = dayStart.ToString("dd/MM"),
                        Posting = dayOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Posting").Sum(o => o.AmountPaid),
                        Featured = dayOrders.Where(o => o.PaidCreditPackage?.CreditType?.Name == "Featured").Sum(o => o.AmountPaid)
                    });
                }
            }

            // Phân Bổ Gói Đã Bán (Ăn theo bộ lọc tuần, tháng, năm)
            var packageColors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Trending", "#10B981" },       // Emerald Green
                { "Tăng Tốc", "#3B82F6" },       // Blue
                { "Bứt Phá", "#8B5CF6" },        // Purple / Violet
                { "Spotlight", "#F59E0B" },      // Gold / Amber
                { "Premium", "#EF4444" },        // Red / Rose
                { "Khởi Động", "#06B6D4" },      // Cyan
                { "Posting Day", "#3B82F6" },
                { "Posting Week", "#8B5CF6" },
                { "Posting Month", "#10B981" },
                { "Featured Day", "#F59E0B" },
                { "Featured Week", "#EF4444" },
                { "Featured Month", "#2D5A3D" }
            };

            var vibrantPalette = new[] { "#10B981", "#3B82F6", "#8B5CF6", "#F59E0B", "#EF4444", "#06B6D4", "#EC4899", "#14B8A6", "#F97316", "#84CC16" };

            var groupedPackages = timeRangeOrders
                .Where(o => o.PaidCreditPackage != null)
                .GroupBy(o => o.PaidCreditPackage!.Name)
                .ToList();

            var packageDistribution = new List<AdminPackageDistributionDto>();
            int colorIdx = 0;
            foreach (var g in groupedPackages)
            {
                string pkgName = g.Key;
                string color = packageColors.ContainsKey(pkgName) ? packageColors[pkgName] : vibrantPalette[colorIdx % vibrantPalette.Length];
                packageDistribution.Add(new AdminPackageDistributionDto
                {
                    Name = pkgName,
                    Value = g.Count(),
                    Color = color
                });
                colorIdx++;
            }

            // Hoạt Động Gần Đây
            var recentActivities = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PaidCreditPackage)
                .Where(o => o.PaymentStatus == PaymentStatus.Successful)
                .OrderByDescending(o => o.PaidAt)
                .Take(8)
                .Select(o => new AdminRecentActivityDto
                {
                    Id = o.OrderCode,
                    User = o.User != null ? o.User.Username : "Unknown",
                    FullName = o.User != null ? o.User.FullName : null,
                    AvatarUrl = o.User != null ? o.User.AvatarUrl : null,
                    Package = o.PaidCreditPackage != null ? o.PaidCreditPackage.Name : "Gói Tín Dụng",
                    Action = o.PaidCreditPackage != null ? $"mua gói {o.PaidCreditPackage.Name}" : "mua gói",
                    Amount = o.AmountPaid,
                    Time = ((o.PaidAt ?? o.CreatedAt).AddHours(7)).ToString("dd/MM/yyyy HH:mm"),
                    Status = "Thành công"
                })
                .ToListAsync();

            return new AdminDashboardResponseDto
            {
                CurrentMonthRevenue = currentMonthRevenue,
                RevenueGrowth = Math.Round(revenueGrowth, 1),
                PackagesSold = packagesSold,
                PackagesSoldGrowth = Math.Round(packagesSoldGrowth, 1),
                TotalUsers = totalUsers,
                TotalUsersGrowth = Math.Round(usersGrowth, 1),
                ActiveProducts = activeProducts,
                ActiveProductsGrowth = Math.Round(productsGrowth, 1),
                RevenueChart = revenueChart,
                RevenueChart7Days = revenueChart,
                PackageDistribution = packageDistribution,
                RecentActivities = recentActivities
            };
        }

        public async Task<int> SendNotificationsAsync(AdminSendNotificationRequestDto request)
        {
            var usersQuery = _context.Users.AsQueryable();

            switch (request.Target.ToLower())
            {
                case "specific":
                    if (request.SpecificUserIds != null && request.SpecificUserIds.Any())
                    {
                        usersQuery = usersQuery.Where(u => request.SpecificUserIds.Contains(u.UserId));
                    }
                    else
                    {
                        return 0; // Không có user nào
                    }
                    break;
                case "active":
                    // Chữ "hoạt động" tạm tính là đã từng đăng nhập hoặc có thao tác.
                    // Nếu bảng User có cột LastLogin thì filter LastLogin >= now - 30 days.
                    // Tạm thời nếu không có thì lấy những user IsOnline hoặc active user
                    usersQuery = usersQuery.Where(u => u.IsActive);
                    break;
                case "new":
                    var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
                    usersQuery = usersQuery.Where(u => u.CreatedAt >= sevenDaysAgo);
                    break;
                case "posting_users":
                    usersQuery = usersQuery.Where(u => _context.UserCreditBatches.Any(b => b.UserId == u.UserId && b.CreditTypeId == 1 && b.IsActive && b.RemainingCredits > 0));
                    break;
                case "featured_users":
                    usersQuery = usersQuery.Where(u => _context.UserCreditBatches.Any(b => b.UserId == u.UserId && b.CreditTypeId == 2 && b.IsActive && b.RemainingCredits > 0));
                    break;
                case "all":
                default:
                    usersQuery = usersQuery.Where(u => u.IsActive);
                    break;
            }

            var userIds = await usersQuery.Select(u => u.UserId).ToListAsync();
            if (!userIds.Any()) return 0;

            var notifications = new List<Notification>();
            var now = DateTime.UtcNow;

            foreach (var userId in userIds)
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Type = request.Type,
                    Title = request.Title,
                    Message = request.Content,
                    IsRead = false,
                    CreatedAt = now,
                    ReferenceId = "system"
                };

                if (request.ScheduledAt.HasValue)
                {
                    notification.ScheduledAt = request.ScheduledAt.Value.ToUniversalTime();
                    notification.IsSent = false;
                }
                else
                {
                    notification.IsSent = true;
                }

                notifications.Add(notification);
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();

            // Push SignalR (only for immediate notifications)
            if (!request.ScheduledAt.HasValue)
            {
                foreach (var notification in notifications)
                {
                    if (ChatHub.UserConnections.TryGetValue(notification.UserId, out var connections))
                    {
                        var payload = new NotificationResponseDto
                        {
                            Id = notification.NotificationId,
                            Type = notification.Type,
                            Title = notification.Title,
                            Message = notification.Message,
                            Time = "Vừa xong",
                            Read = false,
                            ReferenceId = notification.ReferenceId
                        };
                        _ = _hubContext.Clients.Clients(connections.ToList()).SendAsync("NewNotification", payload);
                    }
                }
            }

            return userIds.Count;
        }

        public async Task<List<UserSearchDto>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<UserSearchDto>();

            var lowerQuery = query.ToLower();
            var users = await _context.Users
                .Where(u => u.IsActive && (
                    u.Username.ToLower().Contains(lowerQuery) || 
                    u.Email.ToLower().Contains(lowerQuery) || 
                    (u.FullName != null && u.FullName.ToLower().Contains(lowerQuery))
                ))
                .Take(20)
                .Select(u => new UserSearchDto
                {
                    Id = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    AvatarUrl = u.AvatarUrl
                })
                .ToListAsync();

            return users;
        }

        public async Task<PagedResult<AdminUserResponseDto>> GetUsersAsync(AdminUserQueryDto query)
        {
            var usersQuery = _context.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var lowerSearch = query.Search.ToLower();
                usersQuery = usersQuery.Where(u => u.Email.ToLower().Contains(lowerSearch) || 
                                                 u.Username.ToLower().Contains(lowerSearch) || 
                                                 (u.FullName != null && u.FullName.ToLower().Contains(lowerSearch)));
            }

            if (query.RoleId.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.RoleId == query.RoleId.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.RoleName))
            {
                usersQuery = usersQuery.Where(u => u.Role != null && u.Role.RoleName.ToLower() == query.RoleName.ToLower());
            }

            if (query.IsActive.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.IsActive == query.IsActive.Value);
            }

            var totalRecords = await usersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)query.PageSize);

            if (query.SortBy?.ToLower() == "oldest")
            {
                usersQuery = usersQuery.OrderBy(u => u.CreatedAt);
            }
            else if (query.SortBy?.ToLower() == "transactions")
            {
                usersQuery = usersQuery.OrderByDescending(u => _context.Orders.Count(o => o.UserId == u.UserId));
            }
            else
            {
                usersQuery = usersQuery.OrderByDescending(u => u.CreatedAt);
            }

            var items = await usersQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(u => new AdminUserResponseDto
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    FullName = u.FullName ?? "",
                    AvatarUrl = u.AvatarUrl,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.RoleName : "Unknown",
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    TradeSuccessCount = _context.Orders.Count(o => o.UserId == u.UserId)
                })
                .ToListAsync();

            return new PagedResult<AdminUserResponseDto>
            {
                Items = items,
                TotalCount = totalRecords,
                CurrentPage = query.Page,
                TotalPages = totalPages
            };
        }

        public async Task<AdminUsersSummaryDto> GetUsersSummaryAsync()
        {
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);

            var totalUsers = await _context.Users.CountAsync();
            var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
            var suspendedUsers = await _context.Users.CountAsync(u => !u.IsActive);
            var adminUsers = await _context.Users.Include(u => u.Role).CountAsync(u => u.Role != null && u.Role.RoleName == "Admin");
            var newUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt >= thisMonthStart);

            return new AdminUsersSummaryDto
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                SuspendedUsers = suspendedUsers,
                AdminUsers = adminUsers,
                NewUsersThisMonth = newUsersThisMonth
            };
        }

        public async Task<bool> ToggleUserStatusAsync(long userId, ToggleUserStatusDto request, long currentAdminId)
        {
            if (userId == currentAdminId)
            {
                throw new ValidationException("Bạn không thể tự khóa tài khoản của chính mình.");
            }

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new NotFoundException("Người dùng không tồn tại.");
            }

            if (user.Role != null && user.Role.RoleName == "Admin")
            {
                throw new ValidationException("Bạn không thể khóa hoặc mở khóa người có cùng vai trò.");
            }

            if (user.IsActive == request.IsActive)
            {
                return true; // No change needed
            }

            user.IsActive = request.IsActive;

            _logger.LogInformation("Admin {AdminId} changed status of User {UserId} to {IsActive}. Reason: {Reason}", currentAdminId, userId, request.IsActive, request.Reason);

            await _auditLogRepository.AddAsync(new AdminAuditLog
            {
                AdminId = currentAdminId,
                TargetUserId = userId,
                Action = request.IsActive ? "UNBAN" : "BAN",
                Reason = request.Reason,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PagedResult<TransactionResponseDto>> GetUserTransactionsAsync(long userId, int page, int pageSize)
        {
            var query = _context.Orders
                .Include(o => o.PaidCreditPackage)
                .ThenInclude(p => p.CreditType)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.PaidAt ?? o.CreatedAt);

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new TransactionResponseDto
                {
                    OrderCode = o.OrderCode,
                    PackageName = o.PaidCreditPackage != null ? o.PaidCreditPackage.Name : "Unknown",
                    Type = o.PaidCreditPackage != null && o.PaidCreditPackage.CreditType != null ? o.PaidCreditPackage.CreditType.Name : "Unknown",
                    Credits = o.PaidCreditPackage != null ? o.PaidCreditPackage.CreditAmount : 0,
                    Amount = o.AmountPaid,
                    Status = o.PaymentStatus == PaymentStatus.Successful ? "Success" : 
                             (o.PaymentStatus == PaymentStatus.Pending ? "Pending" : "Failed"),
                    CreatedAt = o.PaidAt ?? o.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<TransactionResponseDto>
            {
                Items = items,
                TotalCount = totalRecords,
                CurrentPage = page,
                TotalPages = totalPages
            };
        }

        public async Task<AdminUserOverviewDto> GetUserOverviewAsync(long userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
            {
                throw new NotFoundException("Người dùng không tồn tại.");
            }

            var postingCredits = await _context.UserCreditBatches
                .Where(b => b.UserId == userId && b.CreditTypeId == 1 && b.IsActive)
                .SumAsync(b => b.RemainingCredits);

            var featuredCredits = await _context.UserCreditBatches
                .Where(b => b.UserId == userId && b.CreditTypeId == 2 && b.IsActive)
                .SumAsync(b => b.RemainingCredits);

            var successfulOrders = await _context.Orders
                .Where(o => o.UserId == userId && o.PaymentStatus == PaymentStatus.Successful)
                .ToListAsync();

            var totalSpent = successfulOrders.Sum(o => o.AmountPaid);
            
            var totalTransactions = await _context.Orders
                .Where(o => o.UserId == userId)
                .CountAsync();

            var productsPosted = await _context.Products
                .Where(p => p.SellerId == userId)
                .CountAsync();

            var recentTransactions = await _context.Orders
                .Include(o => o.PaidCreditPackage)
                .ThenInclude(p => p.CreditType)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.PaidAt ?? o.CreatedAt)
                .Take(4)
                .Select(o => new TransactionResponseDto
                {
                    OrderCode = o.OrderCode,
                    PackageName = o.PaidCreditPackage != null ? o.PaidCreditPackage.Name : "Unknown",
                    Type = o.PaidCreditPackage != null && o.PaidCreditPackage.CreditType != null ? o.PaidCreditPackage.CreditType.Name : "Unknown",
                    Credits = o.PaidCreditPackage != null ? o.PaidCreditPackage.CreditAmount : 0,
                    Amount = o.AmountPaid,
                    Status = o.PaymentStatus == PaymentStatus.Successful ? "Success" : 
                             (o.PaymentStatus == PaymentStatus.Pending ? "Pending" : "Failed"),
                    CreatedAt = o.PaidAt ?? o.CreatedAt
                })
                .ToListAsync();

            return new AdminUserOverviewDto
            {
                PostingCredits = postingCredits,
                FeaturedCredits = featuredCredits,
                TotalSpent = totalSpent,
                ProductsPosted = productsPosted,
                TotalTransactions = totalTransactions,
                RecentTransactions = recentTransactions
            };
        }

        public async Task<List<REVORA_BE.DTOs.Response.BadgeResponseDto>> GetBadgesAsync()
        {
            var badges = await _context.Badges.ToListAsync();
            return badges.Select(b => new REVORA_BE.DTOs.Response.BadgeResponseDto
            {
                BadgeId = b.BadgeId,
                Name = b.Name,
                IconUrl = b.IconUrl,
                Description = b.Description
            }).ToList();
        }
    }
}
