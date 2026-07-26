using Microsoft.EntityFrameworkCore;
using REVORA_BE.DTOs.Request;
using REVORA_BE.Models;
using REVORA_BE.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Implementations
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;

        public ProductRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserCreditBatch>> GetActiveCreditBatchesAsync(long userId, int creditTypeId, bool onlyPermanent = false)
        {
            var query = _context.UserCreditBatches
                .Where(b => b.UserId == userId
                         && b.CreditTypeId == creditTypeId
                         && b.IsActive
                         && b.RemainingCredits > 0
                         && (b.ExpiresAt == null || b.ExpiresAt > DateTime.UtcNow));

            if (onlyPermanent)
            {
                query = query.Where(b => b.ExpiresAt == null);
            }

            return await query
                .OrderByDescending(b => b.ExpiresAt.HasValue) // Dùng gói có hạn trước
                .ThenBy(b => b.ExpiresAt) // Dùng gói sắp hết hạn trước
                .ToListAsync();
        }

        public async Task CreateProductWithTransactionAsync(Product product, Short? shortVideo, List<UserCreditBatch> updatedBatches, List<CreditUsageLog>? usageLogs = null)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Lưu sản phẩm & hình ảnh
                    await _context.Products.AddAsync(product);
                    await _context.SaveChangesAsync();

                    // 2. Lưu Short (nếu có)
                    if (shortVideo != null)
                    {
                        shortVideo.ProductId = product.ProductId;
                        await _context.Shorts.AddAsync(shortVideo);
                    }

                    // 3. Cập nhật lại số lượng Credits
                    if (updatedBatches != null && updatedBatches.Any())
                    {
                        _context.UserCreditBatches.UpdateRange(updatedBatches);
                    }

                    // 4. Lưu lịch sử sử dụng Credit
                    if (usageLogs != null && usageLogs.Any())
                    {
                        foreach (var log in usageLogs)
                        {
                            log.ProductId = product.ProductId;
                        }
                        await _context.CreditUsageLogs.AddRangeAsync(usageLogs);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        public async Task RenewProductWithTransactionAsync(Product product, List<UserCreditBatch> updatedBatches, List<CreditUsageLog>? usageLogs = null)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Products.Update(product);

                    var shortVideo = product.Shorts.FirstOrDefault();
                    if (shortVideo != null)
                    {
                        _context.Shorts.Update(shortVideo);
                    }

                    if (updatedBatches != null && updatedBatches.Any())
                    {
                        _context.UserCreditBatches.UpdateRange(updatedBatches);
                    }

                    if (usageLogs != null && usageLogs.Any())
                    {
                        foreach (var log in usageLogs)
                        {
                            log.ProductId = product.ProductId;
                        }
                        await _context.CreditUsageLogs.AddRangeAsync(usageLogs);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task<List<Product>> GetHomeProductsAsync(string type, int limit)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Seller)
                .Include(p => p.Wishlists)
                .Where(p => p.ProductStatus == "Public" && p.ProductExpiredAt > DateTime.UtcNow);

            switch (type.ToLower())
            {
                case "featured":
                    query = query.Where(p => p.HighlightStatus == true && p.HighlightExpiredAt > DateTime.UtcNow)
                                 .OrderByDescending(p => p.ProductCreateAt);
                    break;
                case "loved":
                    query = query.OrderByDescending(p => p.Wishlists.Count());
                    break;
                case "most-viewed":
                    query = query.OrderByDescending(p => p.ViewCount);
                    break;
                case "newest":
                default:
                    query = query.OrderByDescending(p => p.ProductCreateAt);
                    break;
            }

            return await query.Take(limit).ToListAsync();
        }

        public async Task<(List<Product> Items, int TotalCount)> GetProductsWithFilterAsync(ProductFilterRequestDto filter)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Seller)
                .Include(p => p.Wishlists)
                .Include(p => p.Shorts)
                .Where(p => p.ProductStatus == "Public" && p.ProductExpiredAt > DateTime.UtcNow)
                .AsQueryable();

            if (!string.IsNullOrEmpty(filter.Keyword))
                query = query.Where(p => p.Title.Contains(filter.Keyword) || p.Description.Contains(filter.Keyword));

            if (filter.CategoryId.HasValue && filter.CategoryId > 0)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            if (!string.IsNullOrEmpty(filter.Brand))
                query = query.Where(p => p.Brand == filter.Brand);

            if (!string.IsNullOrEmpty(filter.Condition))
                query = query.Where(p => p.Condition == filter.Condition);

            if (!string.IsNullOrEmpty(filter.City))
                query = query.Where(p => p.Seller.City == filter.City);

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            // Xử lý filter đặc biệt (featured) trước khi count
            if (filter.SortBy?.ToLower() == "featured")
            {
                query = query.Where(p => p.HighlightStatus == true && p.HighlightExpiredAt > DateTime.UtcNow);
            }

            int totalCount = await query.CountAsync();

            switch (filter.SortBy?.ToLower())
            {
                case "priceasc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "pricedesc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "popular":
                case "loved":
                    query = query.OrderByDescending(p => p.Wishlists.Count());
                    break;
                case "featured":
                    query = query.OrderByDescending(p => p.ProductCreateAt);
                    break;
                case "newest":
                default:
                    query = query.OrderByDescending(p => p.ProductCreateAt);
                    break;
            }

            var items = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<Product> GetProductByIdAsync(long productId)
        {
            return await _context.Products
                .Include(p => p.Seller)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Shorts)
                .Include(p => p.Wishlists)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        public async Task<(List<Product> Items, int TotalCount)> GetProductsBySellerAsync(long sellerId, string status, int pageNumber, int pageSize, System.Threading.CancellationToken ct, bool isManageMode = false)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Seller)
                .Include(p => p.Wishlists)
                .Include(p => p.Shorts)
                .AsQueryable();

            if (isManageMode)
            {
                var deletedStatuses = new[] { "Deleted", "AdminDeleted" };
                query = query.Where(p => p.SellerId == sellerId && !deletedStatuses.Contains(p.ProductStatus));
                if (status == "public")
                {
                    query = query.Where(p => p.ProductStatus == "Public" && (p.ProductExpiredAt == null || p.ProductExpiredAt > DateTime.UtcNow));
                }
                else if (status == "private")
                {
                    query = query.Where(p => p.ProductStatus == "Private" && (p.ProductExpiredAt == null || p.ProductExpiredAt > DateTime.UtcNow));
                }
                else if (status == "expired")
                {
                    query = query.Where(p => p.ProductExpiredAt != null && p.ProductExpiredAt <= DateTime.UtcNow);
                }
                else if (status == "violated")
                {
                    var violatedStatuses = new[] { "Violated", "AppealPending" };
                    query = query.Where(p => violatedStatuses.Contains(p.ProductStatus));
                }
                else if (status == "premium")
                {
                    query = query.Where(p => p.ProductStatus == "Public" && 
                                             (p.ProductExpiredAt == null || p.ProductExpiredAt > DateTime.UtcNow) && 
                                             ((p.BannerExpiredAt != null && p.BannerExpiredAt > DateTime.UtcNow) || 
                                              (p.HighlightExpiredAt != null && p.HighlightExpiredAt > DateTime.UtcNow) || 
                                              p.Shorts.Any(s => s.ExpiredAt > DateTime.UtcNow)));
                }
                else if (status == "normal")
                {
                    query = query.Where(p => p.ProductStatus == "Public" && 
                                             (p.ProductExpiredAt == null || p.ProductExpiredAt > DateTime.UtcNow) && 
                                             !((p.BannerExpiredAt != null && p.BannerExpiredAt > DateTime.UtcNow) || 
                                               (p.HighlightExpiredAt != null && p.HighlightExpiredAt > DateTime.UtcNow) || 
                                               p.Shorts.Any(s => s.ExpiredAt > DateTime.UtcNow)));
                }
            }
            else
            {
                query = query.Where(p => p.SellerId == sellerId && p.ProductStatus == "Public" && p.ProductExpiredAt > DateTime.UtcNow);
            }

            int totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.ProductCreateAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<(List<Product> Items, int TotalCount)> GetDeletedProductsBySellerAsync(long sellerId, int pageNumber, int pageSize, System.Threading.CancellationToken ct)
        {
            var deletedStatuses = new[] { "Deleted", "AdminDeleted" };
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Seller)
                .Include(p => p.Wishlists)
                .Include(p => p.Shorts)
                .Where(p => p.SellerId == sellerId && deletedStatuses.Contains(p.ProductStatus))
                .AsQueryable();

            int totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(p => p.ProductCreateAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return (items, totalCount);
        }

        public async Task<bool> UpdateProductAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(long productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            product.ProductStatus = "Deleted";
            product.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeProductStatusAsync(long productId, string status)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return false;

            product.ProductStatus = status;

            if (status == "Public" || status == "Private")
            {
                product.DeletedAt = null;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ProductComment>> GetCommentsByProductIdAsync(long productId)
        {
            return await _context.ProductComments
                .Include(c => c.User)
                .Include(c => c.CommentLikes)
                .Where(c => c.ProductId == productId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<ProductComment?> GetCommentByIdAsync(long commentId)
        {
            return await _context.ProductComments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);
        }

        public async Task<User?> GetUserByIdAsync(long userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<ProductComment> AddCommentAsync(ProductComment comment)
        {
            _context.ProductComments.Add(comment);
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<ProductComment> EditCommentAsync(long commentId, string newContent)
        {
            var comment = await _context.ProductComments.FindAsync(commentId);
            if (comment == null) return null;

            comment.Content = newContent;
            await _context.SaveChangesAsync();
            return comment;
        }

        public async Task<bool> DeleteCommentAsync(long commentId)
        {
            var comment = await _context.ProductComments.FindAsync(commentId);
            if (comment == null) return false;

            // Đệ quy xóa cả comment con
            var children = await _context.ProductComments.Where(c => c.ParentId == commentId).ToListAsync();
            foreach (var child in children)
            {
                await DeleteCommentAsync(child.CommentId);
            }

            _context.ProductComments.Remove(comment);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleLikeCommentAsync(long commentId, long userId)
        {
            var comment = await _context.ProductComments.FindAsync(commentId);
            if (comment == null) return false;

            var like = await _context.ProductCommentLikes
                .FirstOrDefaultAsync(l => l.CommentId == commentId && l.UserId == userId);

            bool isLikedNow;
            if (like != null)
            {
                _context.ProductCommentLikes.Remove(like);
                comment.LikeCount = Math.Max(0, comment.LikeCount - 1);
                isLikedNow = false;
            }
            else
            {
                _context.ProductCommentLikes.Add(new ProductCommentLike
                {
                    CommentId = commentId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                });
                comment.LikeCount += 1;
                isLikedNow = true;
            }

            await _context.SaveChangesAsync();
            return isLikedNow;
        }

        public async Task<bool> IsFollowingAsync(long followerId, long followeeId)
        {
            return await _context.UserFollows
                .AnyAsync(f => f.FollowerId == followerId && f.FolloweeId == followeeId);
        }

        public async Task<Badge?> GetBadgeByIdAsync(int badgeId)
        {
            return await _context.Badges.FindAsync(badgeId);
        }

        public async Task<Dictionary<int, string>> GetBadgeIdToNameMapAsync()
        {
            return await _context.Badges
                .AsNoTracking()
                .ToDictionaryAsync(b => b.BadgeId, b => b.Name);
        }

        public async Task<List<long>> GetAdminUserIdsAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Where(u => u.Role.RoleName == "Admin")
                .Select(u => u.UserId)
                .ToListAsync();
        }
    }
}