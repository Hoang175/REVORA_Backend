using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using REVORA_BE.Constants;
using REVORA_BE.Models;
using REVORA_BE.Models.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Data
{
    public interface ISeedData
    {
        Task SeedAsync();
    }

    public class SeedData : ISeedData
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SeedData> _logger;

        public SeedData(AppDbContext context, ILogger<SeedData> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            // Sử dụng Strategy để an toàn cho cơ chế EnableRetryOnFailure của PostgreSQL AWS
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Seed Roles
                    if (!await _context.Roles.AnyAsync())
                    {
                        _context.Roles.AddRange(
                            new Role { RoleName = "Admin" },
                            new Role { RoleName = "User" }
                        );
                        await _context.SaveChangesAsync();
                    }

                    var adminRole = (await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin"))!;
                    var userRole = (await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User"))!;

                    // 2. Seed Permissions
                    var allPermissionStrings = PermissionConstants.GetAllPermissions();
                    var existingPermissions = await _context.Permissions.ToDictionaryAsync(p => p.Name);

                    foreach (var permStr in allPermissionStrings)
                    {
                        if (!existingPermissions.ContainsKey(permStr))
                        {
                            var newPerm = new Permission
                            {
                                Name = permStr,
                                Description = $"Grants access to {permStr}"
                            };
                            _context.Permissions.Add(newPerm);
                            existingPermissions[permStr] = newPerm;
                        }
                    }

                    // Save changes to generate Permission IDs
                    await _context.SaveChangesAsync();

                    // 3. Seed RolePermissions junction table
                    var allPermissionEntities = existingPermissions.Values.ToList();

                    // Mapping for Admin role (All Permissions)
                    var adminRolePermissions = await _context.RolePermissions
                        .Where(rp => rp.RoleId == adminRole.RoleId)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync();

                    foreach (var perm in allPermissionEntities)
                    {
                        if (!adminRolePermissions.Contains(perm.PermissionId))
                        {
                            _context.RolePermissions.Add(new RolePermission
                            {
                                RoleId = adminRole.RoleId,
                                PermissionId = perm.PermissionId
                            });
                        }
                    }

                    // Mapping for User role (Basic Operational Permissions only)
                    var userRolePermissionStrings = PermissionConstants.GetUserRolePermissions().ToHashSet();
                    var userRolePermissions = await _context.RolePermissions
                        .Where(rp => rp.RoleId == userRole.RoleId)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync();

                    foreach (var perm in allPermissionEntities)
                    {
                        if (userRolePermissionStrings.Contains(perm.Name))
                        {
                            if (!userRolePermissions.Contains(perm.PermissionId))
                            {
                                _context.RolePermissions.Add(new RolePermission
                                {
                                    RoleId = userRole.RoleId,
                                    PermissionId = perm.PermissionId
                                });
                            }
                        }
                    }
                    await _context.SaveChangesAsync();

                    // =========================================================================
                    // DỒN DỮ LIỆU TỪ FILE SEEDDATA CŨ SANG ĐÂY (Chạy an toàn bất đồng bộ)
                    // =========================================================================
                    // 2. Seed Badges
                    var ecoWarriorBadge = _context.Badges.FirstOrDefault(b => b.Name == "Newbie" || b.Name == "Eco Warrior");
                    if (ecoWarriorBadge == null)
                    {
                        ecoWarriorBadge = new Badge { Name = "Eco Warrior", IconUrl = "🌱", Description = "Huy hiệu dành cho tất cả tân thủ bảo vệ môi trường" };
                        _context.Badges.Add(ecoWarriorBadge);
                    }
                    else
                    {
                        ecoWarriorBadge.Name = "Eco Warrior";
                        ecoWarriorBadge.IconUrl = "🌱";
                        ecoWarriorBadge.Description = "Huy hiệu dành cho tất cả tân thủ bảo vệ môi trường";
                    }

                    var topSellerBadge = _context.Badges.FirstOrDefault(b => b.Name == "Top-Seller" || b.Name == "Top Seller");
                    if (topSellerBadge == null)
                    {
                        topSellerBadge = new Badge { Name = "Top Seller", IconUrl = "🏆", Description = "Người bán hàng xuất sắc có doanh số cao" };
                        _context.Badges.Add(topSellerBadge);
                    }
                    else
                    {
                        topSellerBadge.Name = "Top Seller";
                        topSellerBadge.IconUrl = "🏆";
                        topSellerBadge.Description = "Người bán hàng xuất sắc có doanh số cao";
                    }

                    var vipBadge = _context.Badges.FirstOrDefault(b => b.Name == "VIP" || b.Name == "VIP Member");
                    if (vipBadge == null)
                    {
                        vipBadge = new Badge { Name = "VIP Member", IconUrl = "👑", Description = "Thành viên VIP thân thiết của hệ thống" };
                        _context.Badges.Add(vipBadge);
                    }
                    else
                    {
                        vipBadge.Name = "VIP Member";
                        vipBadge.IconUrl = "👑";
                        vipBadge.Description = "Thành viên VIP thân thiết của hệ thống";
                    }

                    // Add other 3 badges if they don't exist
                    var verifiedBadge = _context.Badges.FirstOrDefault(b => b.Name == "Verified");
                    if (verifiedBadge == null)
                    {
                        verifiedBadge = new Badge { Name = "Verified", IconUrl = "✓", Description = "Tích xanh verify tài khoản chính chủ" };
                        _context.Badges.Add(verifiedBadge);
                    }

                    var premiumGoldBadge = _context.Badges.FirstOrDefault(b => b.Name == "Premium Gold");
                    if (premiumGoldBadge == null)
                    {
                        premiumGoldBadge = new Badge { Name = "Premium Gold", IconUrl = "⭐", Description = "Huy hiệu Premium Gold cao cấp" };
                        _context.Badges.Add(premiumGoldBadge);
                    }

                    var trendsetterBadge = _context.Badges.FirstOrDefault(b => b.Name == "Trendsetter");
                    if (trendsetterBadge == null)
                    {
                        trendsetterBadge = new Badge { Name = "Trendsetter", IconUrl = "💎", Description = "Người dẫn đầu xu hướng thời trang" };
                        _context.Badges.Add(trendsetterBadge);
                    }

                    _context.SaveChanges();

                    // 3. Seed CreditTypes
                    if (!_context.CreditTypes.Any())
                    {
                        _context.CreditTypes.AddRange(
                            new CreditType { Name = "Posting" },
                            new CreditType { Name = "Featured" }
                        );
                        _context.SaveChanges();
                    }

                    var postingCreditType = _context.CreditTypes.FirstOrDefault(c => c.Name == "Posting")!;
                    var featuredCreditType = _context.CreditTypes.FirstOrDefault(c => c.Name == "Featured")!;

                    // 4. Seed Categories (Upsert)
                    var catData = new[]
                    {
                        (Name: "Quần Áo", IconUrl: "https://revora.com/categories/clothes.png"),
                        (Name: "Giày Dép", IconUrl: "https://revora.com/categories/shoes.png"),
                        (Name: "Túi Xách", IconUrl: "https://revora.com/categories/bags.png"),
                        (Name: "Phụ Kiện", IconUrl: "https://revora.com/categories/accessories.png"),
                        (Name: "Đồng Hồ", IconUrl: "https://revora.com/categories/watches.png"),
                        (Name: "Kính Mắt", IconUrl: "https://revora.com/categories/glasses.png")
                    };

                    foreach (var data in catData)
                    {
                        var cat = _context.Categories.FirstOrDefault(c => c.IconUrl == data.IconUrl);
                        if (cat == null)
                        {
                            _context.Categories.Add(new Category { Name = data.Name, IconUrl = data.IconUrl, IsActive = true });
                        }
                        else
                        {
                            cat.Name = data.Name;
                        }
                    }
                    _context.SaveChanges();

                    var catQuanAo = _context.Categories.FirstOrDefault(c => c.IconUrl == "https://revora.com/categories/clothes.png")!;
                    var catGiayDep = _context.Categories.FirstOrDefault(c => c.IconUrl == "https://revora.com/categories/shoes.png")!;
                    var catTuiXach = _context.Categories.FirstOrDefault(c => c.IconUrl == "https://revora.com/categories/bags.png")!;
                    var catPhuKien = _context.Categories.FirstOrDefault(c => c.IconUrl == "https://revora.com/categories/accessories.png")!;
                    var catDongHo = _context.Categories.FirstOrDefault(c => c.IconUrl == "https://revora.com/categories/watches.png")!;
                    var catKinhMat = _context.Categories.FirstOrDefault(c => c.IconUrl == "https://revora.com/categories/glasses.png")!;

                    // 5. Seed Users
                    if (!_context.Users.Any())
                    {
                        var hashedPwd = HashPassword("123");
                        _context.Users.AddRange(
                            new User
                            {
                                Username = "admin",
                                Email = "admin@revora.com",
                                PasswordHash = hashedPwd,
                                FullName = "System Administrator",
                                Phone = "0900000001",
                                AvatarUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781006228/REVORA_Media/Avatars/User_2/uigyqu59ceq8pkbeauh2.jpg",
                                Bio = "Tài khoản quản trị hệ thống",
                                Birthday = new DateTime(1990, 1, 1),
                                Gender = "Male",
                                Address = "123 Đường chính",
                                City = "Hồ Chí Minh",
                                RoleId = adminRole.RoleId,
                                IsActive = true,
                                IsOnline = false,
                                IsFirstLogin = false,
                                CreatedAt = DateTime.UtcNow
                            },
                            new User
                            {
                                Username = "user1",
                                Email = "user1@gmail.com",
                                PasswordHash = hashedPwd,
                                FullName = "Nguyễn Văn A (Test User)",
                                Phone = "0900000002",
                                AvatarUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781006228/REVORA_Media/Avatars/User_2/uigyqu59ceq8pkbeauh2.jpg",
                                Bio = "Chuyên bán đồ thời trang Local Brand",
                                Birthday = new DateTime(1998, 5, 15),
                                Gender = "Male",
                                Address = "456 Đường phụ",
                                City = "Hà Nội",
                                RoleId = userRole.RoleId,
                                IsActive = true,
                                IsOnline = false,
                                IsFirstLogin = true,
                                CreatedAt = DateTime.UtcNow
                            },
                            new User
                            {
                                Username = "user2",
                                Email = "user2@gmail.com",
                                PasswordHash = hashedPwd,
                                FullName = "Trần Thị B",
                                Phone = "0900000003",
                                AvatarUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781006228/REVORA_Media/Avatars/User_2/uigyqu59ceq8pkbeauh2.jpg",
                                Bio = "Thanh lý quần áo giá rẻ",
                                Birthday = new DateTime(2000, 10, 20),
                                Gender = "Female",
                                Address = "789 Đường hẻm",
                                City = "Đà Nẵng",
                                RoleId = userRole.RoleId,
                                IsActive = true,
                                IsOnline = false,
                                IsFirstLogin = true,
                                CreatedAt = DateTime.UtcNow
                            }
                        );
                        _context.SaveChanges();
                    }

                    // --- NEW USERS AND ADMINS ---
                    if (!_context.Users.Any(u => u.Username == "huyhoang"))
                    {
                        var hashedPwd = HashPassword("123");
                        var avatarUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781006228/REVORA_Media/Avatars/User_2/uigyqu59ceq8pkbeauh2.jpg";
                        _context.Users.AddRange(
                            // 6 Users
                            new User { Username = "huyhoang", Email = "huyhoang@gmail.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Huy Hoang", Phone = "0901000001", RoleId = userRole.RoleId, IsActive = true, IsFirstLogin = true, CreatedAt = DateTime.UtcNow },
                            new User { Username = "dieulinh", Email = "dieulinh@gmail.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Dieu Linh", Phone = "0901000002", RoleId = userRole.RoleId, IsActive = true, IsFirstLogin = true, CreatedAt = DateTime.UtcNow },
                            new User { Username = "phuonganh", Email = "phuonganh@gmail.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Phuong Anh", Phone = "0901000003", RoleId = userRole.RoleId, IsActive = true, IsFirstLogin = true, CreatedAt = DateTime.UtcNow },
                            new User { Username = "haiyen", Email = "haiyen@gmail.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Hai Yen", Phone = "0901000004", RoleId = userRole.RoleId, IsActive = true, IsFirstLogin = true, CreatedAt = DateTime.UtcNow },
                            new User { Username = "xuanchien", Email = "xuanchien@gmail.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Xuan Chien", Phone = "0901000005", RoleId = userRole.RoleId, IsActive = true, IsFirstLogin = true, CreatedAt = DateTime.UtcNow },
                            new User { Username = "hothanh", Email = "hothanh@gmail.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Ho Thanh", Phone = "0901000006", RoleId = userRole.RoleId, IsActive = true, IsFirstLogin = true, CreatedAt = DateTime.UtcNow },
                            // 4 Admins
                            new User { Username = "admin1", Email = "admin1@revora.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Admin 1", Phone = "0902000001", RoleId = adminRole.RoleId, IsActive = true, IsFirstLogin = false, CreatedAt = DateTime.UtcNow },
                            new User { Username = "admin2", Email = "admin2@revora.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Admin 2", Phone = "0902000002", RoleId = adminRole.RoleId, IsActive = true, IsFirstLogin = false, CreatedAt = DateTime.UtcNow },
                            new User { Username = "admin3", Email = "admin3@revora.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Admin 3", Phone = "0902000003", RoleId = adminRole.RoleId, IsActive = true, IsFirstLogin = false, CreatedAt = DateTime.UtcNow },
                            new User { Username = "admin4", Email = "admin4@revora.com", PasswordHash = hashedPwd, AvatarUrl = avatarUrl, FullName = "Admin 4", Phone = "0902000004", RoleId = adminRole.RoleId, IsActive = true, IsFirstLogin = false, CreatedAt = DateTime.UtcNow }
                        );
                        _context.SaveChanges();
                    }
                    // -----------------------------

                    var adminUser = _context.Users.FirstOrDefault(u => u.Username == "admin")!;
                    var user1 = _context.Users.FirstOrDefault(u => u.Username == "user1")!;
                    var user2 = _context.Users.FirstOrDefault(u => u.Username == "user2")!;

                    // 6. Seed Follows
                    if (!_context.UserFollows.Any())
                    {
                        _context.UserFollows.AddRange(
                            new UserFollow { FollowerId = user1.UserId, FolloweeId = user2.UserId, CreatedAt = DateTime.UtcNow },
                            new UserFollow { FollowerId = user2.UserId, FolloweeId = user1.UserId, CreatedAt = DateTime.UtcNow }
                        );
                        _context.SaveChanges();
                    }

                    // 7. Seed PaidCreditPackages & FreeCreditPackages (Upsert)
                    var paidPacksData = new[]
                    {
                        (Name: "Khởi Động", CreditTypeId: postingCreditType.CreditTypeId, Amount: 1, Price: 10000, Discount: 0, Discounted: 10000, Badge: (int?)null, BadgeDuration: (int?)null),
                        (Name: "Tăng Tốc", CreditTypeId: postingCreditType.CreditTypeId, Amount: 10, Price: 100000, Discount: 20, Discounted: 80000, Badge: (int?)null, BadgeDuration: (int?)null),
                        (Name: "Bứt Phá", CreditTypeId: postingCreditType.CreditTypeId, Amount: 35, Price: 350000, Discount: 30, Discounted: 249999, Badge: (int?)vipBadge.BadgeId, BadgeDuration: (int?)30),
                        (Name: "Spotlight", CreditTypeId: featuredCreditType.CreditTypeId, Amount: 3, Price: 40000, Discount: 0, Discounted: 40000, Badge: (int?)null, BadgeDuration: (int?)null),
                        (Name: "Trending", CreditTypeId: featuredCreditType.CreditTypeId, Amount: 24, Price: 320000, Discount: 22, Discounted: 249999, Badge: (int?)null, BadgeDuration: (int?)null),
                        (Name: "Premium", CreditTypeId: featuredCreditType.CreditTypeId, Amount: 69, Price: 920000, Discount: 30, Discounted: 649999, Badge: (int?)premiumGoldBadge.BadgeId, BadgeDuration: (int?)30)
                    };

                    foreach (var p in paidPacksData)
                    {
                        var pack = _context.PaidCreditPackages.FirstOrDefault(x => x.CreditTypeId == p.CreditTypeId && x.CreditAmount == p.Amount);
                        if (pack == null)
                        {
                            _context.PaidCreditPackages.Add(new PaidCreditPackage { Name = p.Name, CreditTypeId = p.CreditTypeId, CreditAmount = p.Amount, OriginalPrice = p.Price, DiscountRate = p.Discount, DiscountedPrice = p.Discounted, RewardBadgeId = p.Badge, BadgeDurationDays = p.BadgeDuration, IsActive = true });
                        }
                        else
                        {
                            pack.Name = p.Name;
                            pack.OriginalPrice = p.Price;
                            pack.DiscountRate = p.Discount;
                            pack.DiscountedPrice = p.Discounted;
                            pack.RewardBadgeId = p.Badge;
                            pack.BadgeDurationDays = p.BadgeDuration;
                        }
                    }
                    _context.SaveChanges();

                    var postingPackStandard = _context.PaidCreditPackages.FirstOrDefault(p => p.CreditAmount == 10 && p.CreditTypeId == postingCreditType.CreditTypeId)!;
                    var postingPackBasic = _context.PaidCreditPackages.FirstOrDefault(p => p.CreditAmount == 1 && p.CreditTypeId == postingCreditType.CreditTypeId)!;
                    var postingPackPremium = _context.PaidCreditPackages.FirstOrDefault(p => p.CreditAmount == 35 && p.CreditTypeId == postingCreditType.CreditTypeId)!;
                    var featuredPackStandard = _context.PaidCreditPackages.FirstOrDefault(p => p.CreditAmount == 24 && p.CreditTypeId == featuredCreditType.CreditTypeId)!;
                    var featuredPackBasic = _context.PaidCreditPackages.FirstOrDefault(p => p.CreditAmount == 3 && p.CreditTypeId == featuredCreditType.CreditTypeId)!;
                    var featuredPackPremium = _context.PaidCreditPackages.FirstOrDefault(p => p.CreditAmount == 69 && p.CreditTypeId == featuredCreditType.CreditTypeId)!;

                    if (!_context.PaidCreditPackageDescriptions.Any(d => d.Content.Contains("đăng tin vĩnh viễn")))
                    {
                        _context.PaidCreditPackageDescriptions.AddRange(
                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackBasic.PaidCreditPackageId, Content = "1 credits đăng tin vĩnh viễn", DisplayOrder = 1 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackBasic.PaidCreditPackageId, Content = "Liên hệ người mua qua chat/Zalo", DisplayOrder = 2 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackBasic.PaidCreditPackageId, Content = "Khả năng hiển thị tiêu chuẩn", DisplayOrder = 3 },

                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackStandard.PaidCreditPackageId, Content = "10 credits đăng tin vĩnh viễn", DisplayOrder = 1 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackStandard.PaidCreditPackageId, Content = "Tiết kiệm 20% so với mua lẻ", DisplayOrder = 2 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackStandard.PaidCreditPackageId, Content = "Tất cả tính năng Gói Cơ Bản", DisplayOrder = 3 },

                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackPremium.PaidCreditPackageId, Content = "35 credits đăng tin vĩnh viễn", DisplayOrder = 1 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackPremium.PaidCreditPackageId, Content = "Tiết kiệm 30% so với mua lẻ", DisplayOrder = 2 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackPremium.PaidCreditPackageId, Content = "Huy hiệu VIP trong 30 ngày", DisplayOrder = 3 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = postingPackPremium.PaidCreditPackageId, Content = "Tất cả tính năng Gói Tiêu Chuẩn", DisplayOrder = 4 },

                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackBasic.PaidCreditPackageId, Content = "3 credits nổi bật vĩnh viễn", DisplayOrder = 1 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackBasic.PaidCreditPackageId, Content = "Mở khóa upload video Shorts", DisplayOrder = 2 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackBasic.PaidCreditPackageId, Content = "Mở khóa hiển thị trên Banner", DisplayOrder = 3 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackBasic.PaidCreditPackageId, Content = "Viền sản phẩm nổi bật", DisplayOrder = 4 },
                            //new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackBasic.PaidCreditPackageId, Content = "Xuất hiện trên BXH Tuần", DisplayOrder = 5 },

                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackStandard.PaidCreditPackageId, Content = "24 credits nổi bật vĩnh viễn", DisplayOrder = 1 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackStandard.PaidCreditPackageId, Content = "Tiết kiệm 22% so với mua lẻ", DisplayOrder = 2 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackStandard.PaidCreditPackageId, Content = "Tất cả tính năng Gói Cơ Bản", DisplayOrder = 3 },

                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackPremium.PaidCreditPackageId, Content = "69 credits nổi bật vĩnh viễn", DisplayOrder = 1 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackPremium.PaidCreditPackageId, Content = "Tiết kiệm 30% so với mua lẻ", DisplayOrder = 2 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackPremium.PaidCreditPackageId, Content = "Huy hiệu Premium Gold trong 30 ngày", DisplayOrder = 3 },
                            new PaidCreditPackageDescription { PaidCreditPackageId = featuredPackPremium.PaidCreditPackageId, Content = "Tất cả tính năng Gói Cơ Bản", DisplayOrder = 4 }
                        );
                        _context.SaveChanges();
                    }

                    var freePacksData = new[]
                    {
                        (Name: "Quà Tặng Tân Thủ", CreditTypeId: postingCreditType.CreditTypeId, Amount: 2, Duration: 60, Badge: ecoWarriorBadge.BadgeId, BadgeDuration: (int?)null),
                        (Name: "Thưởng Top 1 Seller tuần", CreditTypeId: featuredCreditType.CreditTypeId, Amount: 50, Duration: 60, Badge: topSellerBadge.BadgeId, BadgeDuration: (int?)7)
                    };

                    foreach (var f in freePacksData)
                    {
                        var pack = _context.FreeCreditPackages.FirstOrDefault(x => x.CreditTypeId == f.CreditTypeId && x.CreditAmount == f.Amount);
                        if (pack == null)
                        {
                            _context.FreeCreditPackages.Add(new FreeCreditPackage { Name = f.Name, CreditTypeId = f.CreditTypeId, CreditAmount = f.Amount, DurationDays = f.Duration, RewardBadgeId = f.Badge, BadgeDurationDays = f.BadgeDuration, IsActive = true });
                        }
                        else
                        {
                            pack.Name = f.Name;
                            pack.DurationDays = f.Duration;
                            pack.RewardBadgeId = f.Badge;
                            pack.BadgeDurationDays = f.BadgeDuration;
                        }
                    }
                    _context.SaveChanges();

                    var welcomeFreePack = _context.FreeCreditPackages.FirstOrDefault(f => f.CreditAmount == 2 && f.CreditTypeId == postingCreditType.CreditTypeId)!;
                    var topSellerFreePack = _context.FreeCreditPackages.FirstOrDefault(f => f.CreditAmount == 50 && f.CreditTypeId == featuredCreditType.CreditTypeId)!;

                    // 8. Cấp 50 Credit Posting & 50 Credit Featured cho User1 để test
                    if (!_context.UserCreditBatches.Any())
                    {
                        _context.UserCreditBatches.AddRange(
                            new UserCreditBatch { UserId = user1.UserId, CreditTypeId = postingCreditType.CreditTypeId, FreePackageId = welcomeFreePack.FreeCreditPackageId, RemainingCredits = 50, ClaimedAt = DateTime.UtcNow, ExpiresAt = null, IsActive = true },
                            new UserCreditBatch { UserId = user1.UserId, CreditTypeId = featuredCreditType.CreditTypeId, PaidPackageId = 1, RemainingCredits = 50, ClaimedAt = DateTime.UtcNow, ExpiresAt = null, IsActive = true },
                            new UserCreditBatch { UserId = user2.UserId, CreditTypeId = postingCreditType.CreditTypeId, FreePackageId = welcomeFreePack.FreeCreditPackageId, RemainingCredits = 2, ClaimedAt = DateTime.UtcNow, ExpiresAt = null, IsActive = true }
                        );
                        _context.SaveChanges();
                    }

                    // Cấp Credit cho 6 user mới (100 posting, 300 featured)
                    var newNames = new[] { "huyhoang", "dieulinh", "phuonganh", "haiyen", "xuanchien", "hothanh" };
                    foreach (var un in newNames)
                    {
                        var usr = _context.Users.FirstOrDefault(u => u.Username == un);
                        if (usr != null && !_context.UserCreditBatches.Any(b => b.UserId == usr.UserId && b.CreditTypeId == postingCreditType.CreditTypeId && b.RemainingCredits == 100))
                        {
                            _context.UserCreditBatches.AddRange(
                                new UserCreditBatch { UserId = usr.UserId, CreditTypeId = postingCreditType.CreditTypeId, FreePackageId = welcomeFreePack.FreeCreditPackageId, RemainingCredits = 100, ClaimedAt = DateTime.UtcNow, ExpiresAt = null, IsActive = true },
                                new UserCreditBatch { UserId = usr.UserId, CreditTypeId = featuredCreditType.CreditTypeId, FreePackageId = topSellerFreePack.FreeCreditPackageId, RemainingCredits = 300, ClaimedAt = DateTime.UtcNow, ExpiresAt = null, IsActive = true }
                            );
                        }
                    }
                    _context.SaveChanges();

                    // 9. Giữ nguyên Seed Orders của bạn
                    if (!_context.Orders.Any(o => o.OrderCode == "REVORA_ORD001"))
                    {
                        var now = DateTime.UtcNow;
                        var payOs = PaymentMethod.PayOS;
                        var pending = PaymentStatus.Pending;
                        var successful = PaymentStatus.Successful;
                        var expired = PaymentStatus.Expired;

                        _context.Orders.AddRange(
                            // --- user2: lịch sử đa dạng ---
                            new Order
                            {
                                OrderCode = "REVORA_ORD001",
                                PayOSOrderCode = 2026001,
                                UserId = user2.UserId,
                                PaidCreditPackageId = postingPackStandard.PaidCreditPackageId,
                                PaymentContent = "Mua goi dang tin 7 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-28),
                                ExpiredAt = now.AddDays(-28).AddMinutes(15),
                                PaidAt = now.AddDays(-28),
                                PaymentStatus = successful,
                                Status = OrderStatus.Completed,
                                AmountPaid = postingPackStandard.DiscountedPrice,
                                ReceivedAmount = postingPackStandard.DiscountedPrice,
                                CreditsGranted = true,
                                ProviderTransactionId = "TX001",
                                ResponseCode = "00",
                                ResponsePaymentContent = "Giao dich thanh cong"
                            },
                            new Order
                            {
                                OrderCode = "REVORA_ORD002",
                                PayOSOrderCode = 2026002,
                                UserId = user2.UserId,
                                PaidCreditPackageId = featuredPackStandard.PaidCreditPackageId,
                                PaymentContent = "Mua goi noi bat 7 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-7),
                                ExpiredAt = now.AddDays(-7).AddMinutes(15),
                                PaidAt = now.AddDays(-7),
                                PaymentStatus = successful,
                                Status = OrderStatus.Completed,
                                AmountPaid = featuredPackStandard.DiscountedPrice,
                                ReceivedAmount = featuredPackStandard.DiscountedPrice,
                                CreditsGranted = true,
                                ProviderTransactionId = "TX002",
                                ResponseCode = "00",
                                ResponsePaymentContent = "Giao dich thanh cong"
                            },
                            new Order
                            {
                                OrderCode = "REVORA_ORD003",
                                PayOSOrderCode = 2026003,
                                UserId = user2.UserId,
                                PaidCreditPackageId = postingPackBasic.PaidCreditPackageId,
                                PaymentContent = "Mua goi dang tin 1 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-45),
                                ExpiredAt = now.AddDays(-45).AddMinutes(15),
                                PaidAt = now.AddDays(-45),
                                PaymentStatus = successful,
                                Status = OrderStatus.Completed,
                                AmountPaid = postingPackBasic.DiscountedPrice,
                                ReceivedAmount = postingPackBasic.DiscountedPrice + 5000,
                                CreditsGranted = true,
                                ProviderTransactionId = "TX003",
                                ResponseCode = "00",
                                ResponsePaymentContent = "Chuyen thua tien — van cong credit"
                            },
                            new Order
                            {
                                OrderCode = "REVORA_ORD004",
                                PayOSOrderCode = 2026004,
                                UserId = user2.UserId,
                                PaidCreditPackageId = postingPackPremium.PaidCreditPackageId,
                                PaymentContent = "Mua goi dang tin 30 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-14),
                                ExpiredAt = now.AddDays(-14).AddMinutes(15),
                                PaidAt = now.AddDays(-14),
                                PaymentStatus = successful,
                                Status = OrderStatus.Completed,
                                AmountPaid = postingPackPremium.DiscountedPrice,
                                ReceivedAmount = 150000,
                                CreditsGranted = false,
                                ProviderTransactionId = "TX004",
                                ResponseCode = "00",
                                ResponsePaymentContent = "Nhan thieu tien — khong cong credit (dop)"
                            },
                            new Order
                            {
                                OrderCode = "REVORA_ORD005",
                                PayOSOrderCode = 2026005,
                                UserId = user2.UserId,
                                PaidCreditPackageId = featuredPackBasic.PaidCreditPackageId,
                                PaymentContent = "Mua goi noi bat 1 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddMinutes(-30),
                                ExpiredAt = now.AddMinutes(15),
                                PaymentStatus = pending,
                                Status = OrderStatus.PendingPayment,
                                AmountPaid = featuredPackBasic.DiscountedPrice,
                                CreditsGranted = false
                            },
                            new Order
                            {
                                OrderCode = "REVORA_ORD006",
                                PayOSOrderCode = 2026006,
                                UserId = user2.UserId,
                                PaidCreditPackageId = postingPackStandard.PaidCreditPackageId,
                                PaymentContent = "Mua goi dang tin 7 ngay (lan 2)",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-3),
                                ExpiredAt = now.AddDays(-3).AddMinutes(15),
                                PaymentStatus = expired,
                                Status = OrderStatus.Cancelled,
                                AmountPaid = postingPackStandard.DiscountedPrice,
                                CreditsGranted = false
                            },

                            // --- user1 ---
                            new Order
                            {
                                OrderCode = "REVORA_ORD007",
                                PayOSOrderCode = 2026007,
                                UserId = user1.UserId,
                                PaidCreditPackageId = postingPackStandard.PaidCreditPackageId,
                                PaymentContent = "Mua goi dang tin 7 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-21),
                                ExpiredAt = now.AddDays(-21).AddMinutes(15),
                                PaidAt = now.AddDays(-21),
                                PaymentStatus = successful,
                                Status = OrderStatus.Completed,
                                AmountPaid = postingPackStandard.DiscountedPrice,
                                ReceivedAmount = postingPackStandard.DiscountedPrice,
                                CreditsGranted = true,
                                ProviderTransactionId = "TX007",
                                ResponseCode = "00",
                                ResponsePaymentContent = "Giao dich thanh cong"
                            },
                            new Order
                            {
                                OrderCode = "REVORA_ORD008",
                                PayOSOrderCode = 2026008,
                                UserId = user1.UserId,
                                PaidCreditPackageId = featuredPackPremium.PaidCreditPackageId,
                                PaymentContent = "Mua goi noi bat 30 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-60),
                                ExpiredAt = now.AddDays(-60).AddMinutes(15),
                                PaidAt = now.AddDays(-60),
                                PaymentStatus = successful,
                                Status = OrderStatus.Completed,
                                AmountPaid = featuredPackPremium.DiscountedPrice,
                                ReceivedAmount = featuredPackPremium.DiscountedPrice,
                                CreditsGranted = true,
                                ProviderTransactionId = "TX008",
                                ResponseCode = "00",
                                ResponsePaymentContent = "Giao dich thanh cong"
                            },
                            new Order
                            {
                                OrderCode = "REVORA_ORD009",
                                PayOSOrderCode = 2026009,
                                UserId = user1.UserId,
                                PaidCreditPackageId = featuredPackStandard.PaidCreditPackageId,
                                PaymentContent = "Mua goi noi bat 7 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddMinutes(-10),
                                ExpiredAt = now.AddMinutes(20),
                                PaymentStatus = pending,
                                Status = OrderStatus.PendingPayment,
                                AmountPaid = featuredPackStandard.DiscountedPrice,
                                CreditsGranted = false
                            },
                            new Order
                            {
                                OrderCode = "REVORA_ORD010",
                                PayOSOrderCode = 2026010,
                                UserId = user1.UserId,
                                PaidCreditPackageId = postingPackBasic.PaidCreditPackageId,
                                PaymentContent = "Mua goi dang tin 1 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-2),
                                ExpiredAt = now.AddDays(-2).AddMinutes(15),
                                PaymentStatus = expired,
                                Status = OrderStatus.Cancelled,
                                AmountPaid = postingPackBasic.DiscountedPrice,
                                CreditsGranted = false
                            },

                            // --- admin ---
                            new Order
                            {
                                OrderCode = "REVORA_ORD011",
                                PayOSOrderCode = 2026011,
                                UserId = adminUser.UserId,
                                PaidCreditPackageId = postingPackPremium.PaidCreditPackageId,
                                PaymentContent = "Mua goi dang tin 30 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-10),
                                ExpiredAt = now.AddDays(-10).AddMinutes(15),
                                PaidAt = now.AddDays(-10),
                                PaymentStatus = successful,
                                Status = OrderStatus.Completed,
                                AmountPaid = postingPackPremium.DiscountedPrice,
                                ReceivedAmount = postingPackPremium.DiscountedPrice,
                                CreditsGranted = true,
                                ProviderTransactionId = "TX011",
                                ResponseCode = "00",
                                ResponsePaymentContent = "Giao dich thanh cong"
                            },
                            new Order
                            {
                                OrderCode = "REVORA_ORD012",
                                PayOSOrderCode = 2026012,
                                UserId = adminUser.UserId,
                                PaidCreditPackageId = featuredPackBasic.PaidCreditPackageId,
                                PaymentContent = "Mua goi noi bat 1 ngay",
                                PaymentMethod = payOs,
                                CreatedAt = now.AddDays(-5),
                                ExpiredAt = now.AddDays(-5).AddMinutes(15),
                                PaidAt = now.AddDays(-5),
                                PaymentStatus = PaymentStatus.Failed,
                                Status = OrderStatus.Cancelled,
                                AmountPaid = featuredPackBasic.DiscountedPrice,
                                ReceivedAmount = 20000,
                                CreditsGranted = false,
                                ProviderTransactionId = "TX012",
                                ResponseCode = "00",
                                ResponsePaymentContent = "Nhan thieu tien — khong cong credit (dop)"
                            }
                        );
                        _context.SaveChanges();
                    }

                    // 10. Seed 12 Products (Tất cả gán cho user1)
                    if (!_context.Products.Any(p => p.Title == "Áo Khoác Da Biker Jacket"))
                    {
                        _context.Products.AddRange(
                            // Quần Áo
                            new Product { SellerId = user1.UserId, CategoryId = catQuanAo.CategoryId, Title = "Áo Khoác Da Biker Jacket", Description = "Áo khoác da form chuẩn, chất liệu cao cấp.", Price = 850000, Brand = "Balenciaga", Condition = "LikeNew", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 1, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = true },
                            new Product { SellerId = user1.UserId, CategoryId = catQuanAo.CategoryId, Title = "Áo Hoodie Form Rộng", Description = "Áo nỉ chân cua mặc siêu ấm.", Price = 250000, Brand = "LocalBrand", Condition = "Used", ProductCreateAt = DateTime.UtcNow.AddHours(-1), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },

                            // Giày Dép
                            new Product { SellerId = user1.UserId, CategoryId = catGiayDep.CategoryId, Title = "Giày Sneaker AF1 Trắng", Description = "Giày trắng cơ bản dễ phối đồ.", Price = 1200000, Brand = "Nike", Condition = "New", ProductCreateAt = DateTime.UtcNow.AddHours(-2), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 1, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = true },
                            new Product { SellerId = user1.UserId, CategoryId = catGiayDep.CategoryId, Title = "Dép Sandal Cao Su", Description = "Đi êm chân, chống trượt tốt.", Price = 150000, Brand = "NoBrand", Condition = "Used", ProductCreateAt = DateTime.UtcNow.AddHours(-3), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },

                            // Túi Xách
                            new Product { SellerId = user1.UserId, CategoryId = catTuiXach.CategoryId, Title = "Túi Tote Canvas Basic", Description = "Túi vải đựng vừa laptop 14 inch.", Price = 80000, Brand = "NoBrand", Condition = "New", ProductCreateAt = DateTime.UtcNow.AddHours(-4), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = true },
                            new Product { SellerId = user1.UserId, CategoryId = catTuiXach.CategoryId, Title = "Túi Đeo Chéo Nữ Tính", Description = "Phù hợp đi tiệc, da mềm.", Price = 350000, Brand = "Charles & Keith", Condition = "LikeNew", ProductCreateAt = DateTime.UtcNow.AddHours(-5), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },

                            // Phụ Kiện
                            new Product { SellerId = user1.UserId, CategoryId = catPhuKien.CategoryId, Title = "Mũ Lưỡi Trai Thể Thao", Description = "Form cứng cáp, màu đen dễ phối.", Price = 120000, Brand = "Adidas", Condition = "Used", ProductCreateAt = DateTime.UtcNow.AddHours(-6), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                            new Product { SellerId = user1.UserId, CategoryId = catPhuKien.CategoryId, Title = "Vòng Cổ Bạc Mảnh", Description = "Bạc 925 sáng bóng không rỉ.", Price = 250000, Brand = "PNJ", Condition = "New", ProductCreateAt = DateTime.UtcNow.AddHours(-7), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },

                            // Đồng Hồ
                            new Product { SellerId = user1.UserId, CategoryId = catDongHo.CategoryId, Title = "Đồng Hồ Nam Dây Da", Description = "Thiết kế thanh lịch, chống nước 3ATM.", Price = 1500000, Brand = "Casio", Condition = "LikeNew", ProductCreateAt = DateTime.UtcNow.AddHours(-8), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                            new Product { SellerId = user1.UserId, CategoryId = catDongHo.CategoryId, Title = "Đồng Hồ Thông Minh G-Shock", Description = "Bền bỉ, cá tính, nhiều tính năng.", Price = 2100000, Brand = "G-Shock", Condition = "Used", ProductCreateAt = DateTime.UtcNow.AddHours(-9), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },

                            // Kính Mắt
                            new Product { SellerId = user1.UserId, CategoryId = catKinhMat.CategoryId, Title = "Kính Râm Thời Trang Đi Biển", Description = "Chống UV400, gọng nhựa nhẹ.", Price = 180000, Brand = "NoBrand", Condition = "New", ProductCreateAt = DateTime.UtcNow.AddHours(-10), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                            new Product { SellerId = user1.UserId, CategoryId = catKinhMat.CategoryId, Title = "Kính Cận Gọng Kim Loại Mảnh", Description = "Sang trọng, tôn dáng khuôn mặt.", Price = 300000, Brand = "Kính Mắt Anna", Condition = "LikeNew", ProductCreateAt = DateTime.UtcNow.AddHours(-11), ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },

                            // Sản phẩm hết hạn (Để test)
                            new Product { SellerId = user1.UserId, CategoryId = catQuanAo.CategoryId, Title = "Áo Len Mùa Đông Cũ", Description = "Hàng 2hand hết hạn test", Price = 90000, Brand = "Local", Condition = "Used", ProductCreateAt = DateTime.UtcNow.AddDays(-65), ProductExpiredAt = DateTime.UtcNow.AddDays(-5), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                            new Product { SellerId = user1.UserId, CategoryId = catGiayDep.CategoryId, Title = "Giày Chạy Bộ Thể Thao (Hết hạn)", Description = "Giày thể thao êm ái", Price = 350000, Brand = "Nike", Condition = "Used", ProductCreateAt = DateTime.UtcNow.AddDays(-35), ProductExpiredAt = DateTime.UtcNow.AddDays(-1), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                            new Product { SellerId = user1.UserId, CategoryId = catTuiXach.CategoryId, Title = "Balo Du Lịch Đi Phượt (Hết hạn)", Description = "Balo to rộng rãi", Price = 200000, Brand = "The North Face", Condition = "Used", ProductCreateAt = DateTime.UtcNow.AddDays(-40), ProductExpiredAt = DateTime.UtcNow.AddDays(-10), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                            new Product { SellerId = user1.UserId, CategoryId = catPhuKien.CategoryId, Title = "Kính Chống Cận (Hết hạn)", Description = "Kính cận chống lóa", Price = 120000, Brand = "NoBrand", Condition = "Used", ProductCreateAt = DateTime.UtcNow.AddDays(-70), ProductExpiredAt = DateTime.UtcNow.AddDays(-10), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                            new Product { SellerId = user1.UserId, CategoryId = catDongHo.CategoryId, Title = "Đồng hồ cơ cổ (Hết hạn)", Description = "Đồng hồ chạy cơ", Price = 800000, Brand = "Seiko", Condition = "Used", ProductCreateAt = DateTime.UtcNow.AddDays(-80), ProductExpiredAt = DateTime.UtcNow.AddDays(-20), ProductStatus = "Public", CommentCount = 0, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false }

                        );
                        _context.SaveChanges();
                    }

                    // 10.5 Thêm đa dạng sản phẩm nổi bật & banner cho các user mới
                    var bannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg";
                    var productImageUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg";
                    var hhUser = _context.Users.FirstOrDefault(u => u.Username == "huyhoang") ?? user1;
                    var dlUser = _context.Users.FirstOrDefault(u => u.Username == "dieulinh") ?? user1;
                    var paUser = _context.Users.FirstOrDefault(u => u.Username == "phuonganh") ?? user1;

                    if (!_context.Products.Any(p => p.Title == "Áo Polo Nam Form Chuẩn"))
                    {
                        var extraProducts = new List<Product>
                    {
                        new Product { SellerId = hhUser.UserId, CategoryId = catQuanAo.CategoryId, Title = "Áo Polo Nam Form Chuẩn", Description = "Chất vải thun cá sấu 4 chiều.", Price = 350000, Brand = "Routine", Condition = "New", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 5, IsUsedBanner = true, BannerUrl = bannerUrl, BannerExpiredAt = DateTime.UtcNow.AddDays(15), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                        new Product { SellerId = hhUser.UserId, CategoryId = catGiayDep.CategoryId, Title = "Giày Thể Thao Nữ", Description = "Đế êm, chống trượt.", Price = 650000, Brand = "Biti's", Condition = "LikeNew", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 2, IsUsedBanner = true, BannerUrl = bannerUrl, BannerExpiredAt = DateTime.UtcNow.AddDays(15), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = true },
                        new Product { SellerId = hhUser.UserId, CategoryId = catTuiXach.CategoryId, Title = "Túi Xách Kẹp Nách", Description = "Da PU cao cấp.", Price = 420000, Brand = "Micocah", Condition = "New", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 10, IsUsedBanner = true, BannerUrl = bannerUrl, BannerExpiredAt = DateTime.UtcNow.AddDays(30), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                        new Product { SellerId = dlUser.UserId, CategoryId = catPhuKien.CategoryId, Title = "Set Khuyên Tai Bạc Ý", Description = "An toàn không kích ứng.", Price = 180000, Brand = "PNJ", Condition = "New", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 8, IsUsedBanner = true, BannerUrl = bannerUrl, BannerExpiredAt = DateTime.UtcNow.AddDays(15), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                        new Product { SellerId = paUser.UserId, CategoryId = catDongHo.CategoryId, Title = "Đồng Hồ Nữ Dây Da", Description = "Chống nước 3ATM.", Price = 1250000, Brand = "Julius", Condition = "New", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(60), ProductStatus = "Public", CommentCount = 3, IsUsedBanner = true, BannerUrl = bannerUrl, BannerExpiredAt = DateTime.UtcNow.AddDays(30), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false }
                    };
                        _context.Products.AddRange(extraProducts);
                        _context.SaveChanges();

                        foreach (var p in extraProducts)
                        {
                            if (!_context.ProductImages.Any(img => img.ProductId == p.ProductId))
                            {
                                _context.ProductImages.Add(new ProductImage
                                {
                                    ProductId = p.ProductId,
                                    ImageUrl = productImageUrl
                                });
                            }
                        }
                        _context.SaveChanges();
                    }

                    // Lấy ra danh sách 12 product vừa tạo
                    var allProducts = _context.Products.OrderBy(p => p.ProductId).ToList();

                    // 11. Seed ProductImages (Tất cả 12 sản phẩm dùng chung link ảnh e8lpudrnz...)
                    if (!_context.ProductImages.Any())
                    {
                        var imageList = new List<ProductImage>();
                        var imageUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg";

                        foreach (var product in allProducts)
                        {
                            imageList.Add(new ProductImage { ProductId = product.ProductId, ImageUrl = imageUrl });
                        }
                        _context.ProductImages.AddRange(imageList);
                        _context.SaveChanges();
                    }

                    // Lấy ra 3 sản phẩm đầu tiên để gán Short
                    var p1 = allProducts.ElementAtOrDefault(0);
                    var p2 = allProducts.ElementAtOrDefault(2); // Lấy xen kẽ cho đa dạng
                    var p3 = allProducts.ElementAtOrDefault(4);

                    // 12. Seed 3 Shorts (Gắn 3 link video khác nhau)
                    if (!_context.Shorts.Any() && p1 != null && p2 != null && p3 != null)
                    {
                        _context.Shorts.AddRange(
                            new Short
                            {
                                SellerId = user1.UserId,
                                ProductId = p1.ProductId,
                                VideoUrl = "https://res.cloudinary.com/dh4ut3b4x/video/upload/v1780554047/REVORA_Media/Shorts/User_2/Có_đi_qua_giông_bão_mới_biết_trân_trọng_những_ngày_bình_yên..._m0xudu.mp4",
                                Caption = "Cận cảnh chất liệu xịn xò của em nó đây nha anh em ơi!",
                                LikeCount = 2,
                                CommentCount = 2,
                                ShortStatus = "Active",
                                CreatedAt = DateTime.UtcNow
                            },
                            new Short
                            {
                                SellerId = user1.UserId,
                                ProductId = p2.ProductId,
                                VideoUrl = "https://res.cloudinary.com/dh4ut3b4x/video/upload/v1780566135/REVORA_Media/Shorts/User_2/Có_đi_qua_giông_bão_mới_biết_trân_trọng_những_ngày_bình_yên..._t0ifvv.mp4",
                                Caption = "Lên chân thực tế siêu tôn dáng luôn",
                                LikeCount = 1,
                                CommentCount = 0,
                                ShortStatus = "Active",
                                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                            },
                            new Short
                            {
                                SellerId = user1.UserId,
                                ProductId = p3.ProductId,
                                VideoUrl = "https://res.cloudinary.com/dh4ut3b4x/video/upload/v1780566462/REVORA_Media/Shorts/User_3/Có_đi_qua_giông_bão_mới_biết_trân_trọng_những_ngày_bình_yên..._sxlz4m.mp4",
                                Caption = "Phối đồ dạo phố đơn giản mà vẫn nổi bật",
                                LikeCount = 3,
                                CommentCount = 1,
                                ShortStatus = "Active",
                                CreatedAt = DateTime.UtcNow.AddHours(-1)
                            }
                        );
                        _context.SaveChanges();
                    }

                    // 13. Tương tác mồi (Comments & Likes)
                    if (!_context.ProductComments.Any() && p1 != null && p2 != null)
                    {
                        _context.ProductComments.AddRange(
                            new ProductComment { ProductId = p1.ProductId, UserId = user2.UserId, Content = "Fix thêm không shop ơi?", LikeCount = 0, CreatedAt = DateTime.UtcNow },
                            new ProductComment { ProductId = p2.ProductId, UserId = user2.UserId, Content = "Còn size 42 không ạ?", LikeCount = 1, CreatedAt = DateTime.UtcNow }
                        );
                        _context.SaveChanges();
                    }

                    var shorts = _context.Shorts.ToList();
                    if (!_context.ShortComments.Any() && shorts.Count >= 3)
                    {
                        _context.ShortComments.AddRange(
                            new ShortComment { ShortId = shorts[0].ShortId, UserId = user2.UserId, Content = "Nhìn chất quá anh trai", LikeCount = 0, CreatedAt = DateTime.UtcNow },
                            new ShortComment { ShortId = shorts[0].ShortId, UserId = adminUser.UserId, Content = "Lên video đẹp lắm", LikeCount = 0, CreatedAt = DateTime.UtcNow },
                            new ShortComment { ShortId = shorts[2].ShortId, UserId = user2.UserId, Content = "Túi xinh nha", LikeCount = 0, CreatedAt = DateTime.UtcNow }
                        );
                        _context.SaveChanges();
                    }

                    if (!_context.ShortLikes.Any() && shorts.Count >= 3)
                    {
                        _context.ShortLikes.AddRange(
                            new ShortLike { ShortId = shorts[0].ShortId, UserId = user2.UserId, CreatedAt = DateTime.UtcNow },
                            new ShortLike { ShortId = shorts[0].ShortId, UserId = adminUser.UserId, CreatedAt = DateTime.UtcNow },
                            new ShortLike { ShortId = shorts[1].ShortId, UserId = user2.UserId, CreatedAt = DateTime.UtcNow },
                            new ShortLike { ShortId = shorts[2].ShortId, UserId = adminUser.UserId, CreatedAt = DateTime.UtcNow }
                        );
                        _context.SaveChanges();
                    }

                    // 14. Seed sản phẩm Match & Trade (chỉ hiển thị)
                    if (!_context.Products.Any(p => p.IsMatchSeed))
                    {
                        var seedImage = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg";
                        var seedProducts = new List<Product>
                    {
                        new Product { SellerId = adminUser.UserId, CategoryId = catQuanAo.CategoryId, Title = "Áo Thun Oversize Vintage", Description = "Áo thun oversize chất liệu cotton mát mẻ, phong cách vintage trẻ trung năng động", Price = 180000, Brand = "Local Brand", Condition = "LikeNew", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(90), ProductStatus = "Public", IsMatchSeed = true, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                        new Product { SellerId = adminUser.UserId, CategoryId = catQuanAo.CategoryId, Title = "Quần Jeans Ống Rộng", Description = "Quần jeans ống rộng phom dáng basic, dễ phối đồ và thoải mái vận động", Price = 250000, Brand = "Levi's", Condition = "Used", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(90), ProductStatus = "Public", IsMatchSeed = true, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                        new Product { SellerId = adminUser.UserId, CategoryId = catTuiXach.CategoryId, Title = "Túi Tote Canvas", Description = "Túi tote vải canvas chắc chắn, phù hợp đi học, đi làm hay đi chơi dạo phố", Price = 120000, Brand = "NoBrand", Condition = "New", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(90), ProductStatus = "Public", IsMatchSeed = true, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                        new Product { SellerId = adminUser.UserId, CategoryId = catPhuKien.CategoryId, Title = "Tai Nghe Bluetooth", Description = "Tai nghe bluetooth không dây kết nối ổn định, chất âm trầm ấm cực hay", Price = 450000, Brand = "Sony", Condition = "LikeNew", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(90), ProductStatus = "Public", IsMatchSeed = true, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false },
                        new Product { SellerId = adminUser.UserId, CategoryId = catGiayDep.CategoryId, Title = "Giày Sneaker Trắng", Description = "Giày sneaker trắng cổ thấp năng động, dễ vệ sinh, phù hợp mọi hoạt động", Price = 380000, Brand = "Nike", Condition = "Used", ProductCreateAt = DateTime.UtcNow, ProductExpiredAt = DateTime.UtcNow.AddDays(90), ProductStatus = "Public", IsMatchSeed = true, IsUsedBanner = true, BannerUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781038062/REVORA_Media/Products/User_4/fjyuaeufrslwlkyc6szh.jpg", BannerExpiredAt = DateTime.UtcNow.AddDays(7), HighlightStatus = true, HighlightExpiredAt = DateTime.UtcNow.AddDays(60), BannerStatus = true, IsUsedShort = false }
                    };
                        _context.Products.AddRange(seedProducts);
                        _context.SaveChanges();

                        foreach (var sp in seedProducts)
                        {
                            _context.ProductImages.Add(new ProductImage { ProductId = sp.ProductId, ImageUrl = seedImage });
                        }
                        _context.SaveChanges();
                    }

                    // 15. Match & Trade Bot Users & Sessions
                    if (!_context.Users.Any(u => u.Email == "bot_hn@revora.com"))
                    {
                        var demoEmail1 = "bot_hn@revora.com";
                        var demoEmail2 = "bot_hcm@revora.com";

                        var bot1 = new User
                        {
                            Username = "bothn",
                            Email = demoEmail1,
                            PasswordHash = HashPassword("dummy"),
                            FullName = "Hà Nội Bot",
                            City = "Hà Nội",
                            RoleId = userRole.RoleId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        var bot2 = new User
                        {
                            Username = "bothcm",
                            Email = demoEmail2,
                            PasswordHash = HashPassword("dummy"),
                            FullName = "Hồ Chí Minh Bot",
                            City = "Hồ Chí Minh",
                            RoleId = userRole.RoleId,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Users.AddRange(bot1, bot2);
                        _context.SaveChanges();

                        var prices = new decimal[] { 200000, 400000, 800000, 1500000 };
                        var bots = new[] { bot1, bot2 };

                        foreach (var bot in bots)
                        {
                            var products = new List<Product>();
                            foreach (var price in prices)
                            {
                                products.Add(new Product
                                {
                                    SellerId = bot.UserId,
                                    CategoryId = catQuanAo.CategoryId,
                                    Title = $"Sản phẩm trao đổi ({bot.City}) - {price / 1000}k",
                                    Price = price,
                                    ProductStatus = "Public",
                                    IsMatchSeed = false,
                                    ProductCreateAt = DateTime.UtcNow,
                                    ProductExpiredAt = DateTime.UtcNow.AddDays(90)
                                });
                            }
                            _context.Products.AddRange(products);
                            _context.SaveChanges();

                            var session = new MatchSession
                            {
                                UserId = bot.UserId,
                                Status = "Active",
                                MinPrice = 0,
                                MaxPrice = 1_000_000_000m,
                                City = null,
                                StartedAt = DateTime.UtcNow
                            };
                            _context.MatchSessions.Add(session);
                            _context.SaveChanges();

                            foreach (var p in products)
                            {
                                _context.MatchSessionProducts.Add(new MatchSessionProduct
                                {
                                    MatchSessionId = session.MatchSessionId,
                                    ProductId = p.ProductId
                                });

                                _context.ProductImages.Add(new ProductImage
                                {
                                    ProductId = p.ProductId,
                                    ImageUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg"
                                });
                            }
                            _context.SaveChanges();
                        }
                    }


                    // 10. Seed Announcements
                    if (!_context.Announcements.Any())
                    {
                        var now = DateTime.UtcNow;
                        _context.Announcements.AddRange(
                        new Announcement
                        {
                            Title = "🔥 REVORA MATCH",
                            Description = "Khám phá cách trao đổi thời trang hoàn toàn mới. Vuốt để tìm người phù hợp, kết nối và thương lượng trực tiếp với cộng đồng REVORA.",
                            ImageUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg",
                            RedirectUrl = "/match",
                            ButtonText = "Khám phá ngay",
                            Priority = 100,
                            StartAt = now,
                            EndAt = now.AddYears(10),
                            IsActive = true,
                            CreatedAt = now
                        },
                        new Announcement
                        {
                            Title = "📦 ĐĂNG TIN CHỈ TRONG VÀI PHÚT",
                            Description = "Biến những món đồ không còn sử dụng thành giá trị mới bằng bài đăng hoặc video Short thu hút người mua.",
                            ImageUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg",
                            RedirectUrl = "/sell",
                            ButtonText = "Đăng tin ngay",
                            Priority = 90,
                            StartAt = now,
                            EndAt = now.AddYears(10),
                            IsActive = true,
                            CreatedAt = now
                        },
                        new Announcement
                        {
                            Title = "🏆 CHINH PHỤC BẢNG XẾP HẠNG",
                            Description = "Gia tăng lượt tương tác, bán được nhiều sản phẩm hơn và cạnh tranh để xuất hiện trong Top Seller nổi bật mỗi tuần.",
                            ImageUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg",
                            RedirectUrl = "/ranking",
                            ButtonText = "Xem bảng xếp hạng",
                            Priority = 80,
                            StartAt = now,
                            EndAt = now.AddYears(10),
                            IsActive = true,
                            CreatedAt = now
                        },
                        new Announcement
                        {
                            Title = "🎯 DỰ ĐOÁN & NHẬN THƯỞNG",
                            Description = "Dự đoán những người bán và sản phẩm sẽ dẫn đầu tuần này để nhận thêm Credit và danh hiệu độc quyền.",
                            ImageUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg",
                            RedirectUrl = "/ranking",
                            ButtonText = "Tham gia dự đoán",
                            Priority = 70,
                            StartAt = now,
                            EndAt = now.AddYears(10),
                            IsActive = true,
                            CreatedAt = now
                        },
                        new Announcement
                        {
                            Title = "✨ KHÁM PHÁ THỜI TRANG THEO CÁCH MỚI",
                            Description = "Lướt xem sản phẩm bằng video Short, theo dõi người bán yêu thích và khám phá hàng nghìn món đồ độc đáo mỗi ngày.",
                            ImageUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg",
                            RedirectUrl = "/shorts",
                            ButtonText = "Khám phá ngay",
                            Priority = 60,
                            StartAt = now,
                            EndAt = now.AddYears(10),
                            IsActive = true,
                            CreatedAt = now
                        },
                        new Announcement
                        {
                            Title = "♻️ CHO THỜI TRANG MỘT CUỘC ĐỜI THỨ HAI",
                            Description = "REVORA giúp kết nối những người yêu thời trang bền vững, nơi mỗi món đồ đều có cơ hội được tiếp tục sử dụng và lan tỏa giá trị.",
                            ImageUrl = "https://res.cloudinary.com/dh4ut3b4x/image/upload/v1781037903/REVORA_Media/Products/User_4/mjitp5f6g9ftqaxra1su.jpg",
                            RedirectUrl = "/",
                            ButtonText = "Tìm hiểu REVORA",
                            Priority = 50,
                            StartAt = now,
                            EndAt = now.AddYears(10),
                            IsActive = true,
                            CreatedAt = now
                        }
                        );
                        _context.SaveChanges();
                    }

                    // Gộp giao dịch thành công
                    await transaction.CommitAsync();
                    _logger.LogInformation("Database seeded successfully with Roles, Permissions, and RolePermissions mapping.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred while seeding the database transactionally.");
                    throw;
                }
            });
        }

        private static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
