using Microsoft.EntityFrameworkCore;

namespace REVORA_BE.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserFollow> UserFollows => Set<UserFollow>();
        public DbSet<Badge> Badges => Set<Badge>();
        public DbSet<UserBadge> UserBadges => Set<UserBadge>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<Wishlist> Wishlists => Set<Wishlist>();
        public DbSet<ProductComment> ProductComments => Set<ProductComment>();
        public DbSet<ProductCommentLike> ProductCommentLikes => Set<ProductCommentLike>();
        public DbSet<Short> Shorts => Set<Short>();
        public DbSet<ShortLike> ShortLikes => Set<ShortLike>();
        public DbSet<ShortComment> ShortComments => Set<ShortComment>();
        public DbSet<ShortCommentLike> ShortCommentLikes => Set<ShortCommentLike>();
        public DbSet<CreditType> CreditTypes => Set<CreditType>();
        public DbSet<PaidCreditPackage> PaidCreditPackages => Set<PaidCreditPackage>();
        public DbSet<PaidCreditPackageDescription> PaidCreditPackageDescriptions => Set<PaidCreditPackageDescription>();
        public DbSet<FreeCreditPackage> FreeCreditPackages => Set<FreeCreditPackage>();
        public DbSet<UserCreditBatch> UserCreditBatches => Set<UserCreditBatch>();
        public DbSet<CreditUsageLog> CreditUsageLogs => Set<CreditUsageLog>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

        // Chat box real  time start
        public DbSet<Conversation> Conversations => Set<Conversation>();
        public DbSet<Message> Messages => Set<Message>();
        // Chat box real  time end

        public DbSet<MatchSession> MatchSessions => Set<MatchSession>();
        public DbSet<MatchSessionProduct> MatchSessionProducts => Set<MatchSessionProduct>();
        public DbSet<MatchSwipe> MatchSwipes => Set<MatchSwipe>();
        public DbSet<MatchInterestNotification> MatchInterestNotifications => Set<MatchInterestNotification>();
        public DbSet<TradeMatch> TradeMatches => Set<TradeMatch>();
        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<Announcement> Announcements => Set<Announcement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Feedback>(entity =>
            {
                entity.HasKey(x => x.FeedbackId);
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(x => x.RoleId);
                entity.Property(x => x.RoleName).IsRequired().HasMaxLength(50);
                entity.HasIndex(x => x.RoleName).IsUnique();
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(x => x.UserId);
                entity.Property(x => x.Email).IsRequired().HasMaxLength(255);
                entity.Property(x => x.Username).IsRequired().HasMaxLength(100);
                entity.Property(x => x.IsActive).HasDefaultValue(true);
                entity.Property(x => x.IsFirstLogin).HasDefaultValue(true);
                entity.HasIndex(x => x.Email).IsUnique();
                entity.HasIndex(x => x.Username).IsUnique();

                entity.HasOne(x => x.Role)
                    .WithMany(x => x.Users)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<UserFollow>()
                .HasKey(x => new { x.FollowerId, x.FolloweeId });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(x => x.PermissionId);
                entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
                entity.HasIndex(x => x.Name).IsUnique();
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(x => new { x.RoleId, x.PermissionId });

                entity.HasOne(x => x.Role)
                    .WithMany(x => x.RolePermissions)
                    .HasForeignKey(x => x.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(x => x.Permission)
                    .WithMany(x => x.RolePermissions)
                    .HasForeignKey(x => x.PermissionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Token).IsRequired().HasMaxLength(512);
                entity.Property(x => x.IsRevoked).HasDefaultValue(false);
                entity.Property(x => x.DeviceName).HasMaxLength(255);
                entity.Property(x => x.IpAddress).HasMaxLength(50);
                entity.HasIndex(x => x.Token).IsUnique();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.RefreshTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Badge>()
                .HasKey(x => x.BadgeId);

            modelBuilder.Entity<UserBadge>()
                .HasKey(x => x.UserBadgeId);

            modelBuilder.Entity<Category>()
                .HasKey(x => x.CategoryId);

            modelBuilder.Entity<Product>()
                .HasKey(x => x.ProductId);

            modelBuilder.Entity<Product>()
                .Property(x => x.Price)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<ProductImage>()
                .HasKey(x => x.ProductImageId);

            modelBuilder.Entity<Wishlist>()
                .HasKey(x => new { x.UserId, x.ProductId });

            modelBuilder.Entity<ProductCommentLike>()
                .HasKey(x => new { x.CommentId, x.UserId });

            modelBuilder.Entity<ShortLike>()
                .HasKey(x => new { x.ShortId, x.UserId });

            modelBuilder.Entity<ProductComment>()
                .HasKey(x => x.CommentId);

            modelBuilder.Entity<ShortComment>()
                .HasKey(x => x.CommentId);

            modelBuilder.Entity<ShortCommentLike>()
                .HasKey(x => new { x.CommentId, x.UserId });

            modelBuilder.Entity<Short>()
                .HasKey(x => x.ShortId);

            modelBuilder.Entity<CreditType>()
                .HasKey(x => x.CreditTypeId);

            modelBuilder.Entity<PaidCreditPackage>()
                .HasKey(x => x.PaidCreditPackageId);

            modelBuilder.Entity<PaidCreditPackage>()
                .Property(x => x.OriginalPrice)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<PaidCreditPackage>()
                .Property(x => x.DiscountRate)
                .HasColumnType("decimal(5, 2)");

            modelBuilder.Entity<PaidCreditPackage>()
                .Property(x => x.DiscountedPrice)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<PaidCreditPackageDescription>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Content).IsRequired();
                
                entity.HasOne(x => x.PaidCreditPackage)
                    .WithMany(x => x.Descriptions)
                    .HasForeignKey(x => x.PaidCreditPackageId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<FreeCreditPackage>()
                .HasKey(x => x.FreeCreditPackageId);

            modelBuilder.Entity<UserCreditBatch>()
                .HasKey(x => x.BatchId);

            modelBuilder.Entity<Order>()
                .HasKey(x => x.OrderId);

            modelBuilder.Entity<Order>()
                .Property(x => x.AmountPaid)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<Order>()
                .HasIndex(x => x.PayOSOrderCode)
                .IsUnique();


            modelBuilder.Entity<Order>()
                .Property(x => x.ReceivedAmount)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<Order>()
                .Property(x => x.CreditsGranted)
                .HasDefaultValue(false);

            modelBuilder.Entity<Product>()
                .HasOne(x => x.Seller)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Product>()
                .HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductImage>()
                .HasOne(x => x.Product)
                .WithMany(x => x.ProductImages)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductComment>()
                .HasOne(x => x.Product)
                .WithMany(x => x.ProductComments)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductComment>()
                .HasOne(x => x.User)
                .WithMany(x => x.ProductComments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductComment>()
                .HasOne(x => x.ParentComment)
                .WithMany(x => x.ChildComments)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProductCommentLike>()
                .HasOne(x => x.ProductComment)
                .WithMany(x => x.CommentLikes)
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProductCommentLike>()
                .HasOne(x => x.User)
                .WithMany(x => x.ProductCommentLikes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Short>()
                .HasOne(x => x.Seller)
                .WithMany(x => x.Shorts)
                .HasForeignKey(x => x.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Short>()
                .HasOne(x => x.Product)
                .WithMany(x => x.Shorts)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ShortComment>()
                .HasOne(x => x.Short)
                .WithMany(x => x.ShortComments)
                .HasForeignKey(x => x.ShortId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShortComment>()
                .HasOne(x => x.User)
                .WithMany(x => x.ShortComments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShortComment>()
                .HasOne(x => x.ParentComment)
                .WithMany(x => x.ChildComments)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ShortLike>()
                .HasOne(x => x.Short)
                .WithMany(x => x.ShortLikes)
                .HasForeignKey(x => x.ShortId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShortLike>()
                .HasOne(x => x.User)
                .WithMany(x => x.ShortLikes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ShortCommentLike>()
                .HasOne(x => x.ShortComment)
                .WithMany()
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ShortCommentLike>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollow>()
                .HasOne(x => x.Follower)
                .WithMany(x => x.Followees)
                .HasForeignKey(x => x.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFollow>()
                .HasOne(x => x.Followee)
                .WithMany(x => x.Followers)
                .HasForeignKey(x => x.FolloweeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Wishlist>()
                .HasOne(x => x.User)
                .WithMany(x => x.Wishlists)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Wishlist>()
                .HasOne(x => x.Product)
                .WithMany(x => x.Wishlists)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Badge>()
                .HasMany(x => x.UserBadges)
                .WithOne(x => x.Badge)
                .HasForeignKey(x => x.BadgeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserBadge>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserBadges)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserCreditBatch>()
                .HasOne(x => x.User)
                .WithMany(x => x.UserCreditBatches)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserCreditBatch>()
                .HasOne(x => x.CreditType)
                .WithMany(x => x.UserCreditBatches)
                .HasForeignKey(x => x.CreditTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserCreditBatch>()
                .HasOne(x => x.PaidCreditPackage)
                .WithMany(x => x.UserCreditBatches)
                .HasForeignKey(x => x.PaidPackageId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<UserCreditBatch>()
                .HasOne(x => x.FreeCreditPackage)
                .WithMany()
                .HasForeignKey(x => x.FreePackageId)
                .OnDelete(DeleteBehavior.SetNull);

            // CẤU HÌNH FILTER INDEX CHUẨN DÀNH RIÊNG CHO POSTGRESQL
            modelBuilder.Entity<UserCreditBatch>(entity =>
            {
                entity.HasIndex(x => x.OrderId)
                    .IsUnique()
                    .HasFilter("\"OrderId\" IS NOT NULL");

                entity.HasOne(x => x.Order)
                    .WithMany()
                    .HasForeignKey(x => x.OrderId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CreditUsageLog>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CreditUsageLog>()
                .HasOne(x => x.CreditType)
                .WithMany()
                .HasForeignKey(x => x.CreditTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PaidCreditPackage>()
                .HasOne(x => x.CreditType)
                .WithMany(x => x.PaidCreditPackages)
                .HasForeignKey(x => x.CreditTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FreeCreditPackage>()
                .HasOne(x => x.CreditType)
                .WithMany(x => x.FreeCreditPackages)
                .HasForeignKey(x => x.CreditTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(x => x.User)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(x => x.PaidCreditPackage)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.PaidCreditPackageId)
                .OnDelete(DeleteBehavior.Restrict);

            // Bổ sung cấu hình RewardBadgeId
            modelBuilder.Entity<PaidCreditPackage>()
                .HasOne(x => x.RewardBadge)
                .WithMany()
                .HasForeignKey(x => x.RewardBadgeId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<FreeCreditPackage>()
                .HasOne(x => x.RewardBadge)
                .WithMany()
                .HasForeignKey(x => x.RewardBadgeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchSession>()
                .HasKey(x => x.MatchSessionId);

            modelBuilder.Entity<MatchSession>()
                .Property(x => x.MinPrice)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<MatchSession>()
                .Property(x => x.MaxPrice)
                .HasColumnType("decimal(18, 2)");

            modelBuilder.Entity<MatchSession>()
                .HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchSessionProduct>()
                .HasKey(x => new { x.MatchSessionId, x.ProductId });

            modelBuilder.Entity<MatchSessionProduct>()
                .HasOne(x => x.MatchSession)
                .WithMany(x => x.OfferingProducts)
                .HasForeignKey(x => x.MatchSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchSessionProduct>()
                .HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchSwipe>()
                .HasKey(x => x.MatchSwipeId);

            modelBuilder.Entity<MatchSwipe>()
                .HasIndex(x => new { x.MatchSessionId, x.TargetProductId })
                .IsUnique();

            modelBuilder.Entity<MatchSwipe>()
                .HasOne(x => x.MatchSession)
                .WithMany(x => x.Swipes)
                .HasForeignKey(x => x.MatchSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MatchSwipe>()
                .HasOne(x => x.TargetProduct)
                .WithMany()
                .HasForeignKey(x => x.TargetProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MatchInterestNotification>(entity =>
            {
                entity.HasKey(x => x.MatchInterestNotificationId);

                entity.HasOne(x => x.OwnerUser)
                    .WithMany()
                    .HasForeignKey(x => x.OwnerUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.InterestedUser)
                    .WithMany()
                    .HasForeignKey(x => x.InterestedUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.LikedProduct)
                    .WithMany()
                    .HasForeignKey(x => x.LikedProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.OfferingProduct)
                    .WithMany()
                    .HasForeignKey(x => x.OfferingProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TradeMatch>()
                .HasKey(x => x.TradeMatchId);

            modelBuilder.Entity<TradeMatch>()
                .HasIndex(x => new { x.UserLowId, x.UserHighId, x.Status });

            modelBuilder.Entity<TradeMatch>()
                .HasOne(x => x.Conversation)
                .WithMany()
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<TradeMatch>()
                .HasOne(x => x.UserLow)
                .WithMany()
                .HasForeignKey(x => x.UserLowId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeMatch>()
                .HasOne(x => x.UserHigh)
                .WithMany()
                .HasForeignKey(x => x.UserHighId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeMatch>()
                .HasOne(x => x.ProductLowUser)
                .WithMany()
                .HasForeignKey(x => x.ProductLowUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TradeMatch>()
                .HasOne(x => x.ProductHighUser)
                .WithMany()
                .HasForeignKey(x => x.ProductHighUserId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);

            // ==========================================
            // CẤU HÌNH CONVERSATION & MESSAGE (CHAT)
            // ==========================================

            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(x => x.ConversationId);

                // Đảm bảo không có 2 cuộc hội thoại trùng lặp giữa 2 người
                entity.HasIndex(x => new { x.User1Id, x.User2Id }).IsUnique();

                entity.HasOne(x => x.User1)
                    .WithMany()
                    .HasForeignKey(x => x.User1Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.User2)
                    .WithMany()
                    .HasForeignKey(x => x.User2Id)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(x => x.MessageId);

                entity.HasOne(x => x.Conversation)
                    .WithMany(x => x.Messages)
                    .HasForeignKey(x => x.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade); // Xóa hội thoại thì xóa luôn tin nhắn

                entity.HasOne(x => x.Sender)
                    .WithMany()
                    .HasForeignKey(x => x.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.ProductRef)
                    .WithMany()
                    .HasForeignKey(x => x.ProductRefId)
                    .OnDelete(DeleteBehavior.SetNull); // Xóa sản phẩm thì tin nhắn vẫn còn, chỉ mất link SP
            }
            
            );

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(x => x.NotificationId);
                
                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<AdminAuditLog>(entity =>
            {
                entity.HasKey(x => x.LogId);
                
                entity.HasOne(x => x.Admin)
                    .WithMany()
                    .HasForeignKey(x => x.AdminId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.TargetUser)
                    .WithMany()
                    .HasForeignKey(x => x.TargetUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }
    }
}
