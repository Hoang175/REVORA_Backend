using Microsoft.EntityFrameworkCore;
using REVORA_BE.Models;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Services.Interfaces;

namespace REVORA_BE.Services.Implementations
{
    public class WishlistService : IWishlistService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public WishlistService(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<bool> ToggleWishlistAsync(long userId, long productId, CancellationToken ct)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId, ct);
            if (product == null) throw new Exception("Product not found");

            var existing = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, ct);

            if (existing != null)
            {
                _context.Wishlists.Remove(existing);
                await _context.SaveChangesAsync(ct);
                return false; // Removed
            }
            else
            {
                var wishlist = new Wishlist
                {
                    UserId = userId,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync(ct);

                // Thêm thông báo
                var userWhoLiked = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
                if (userWhoLiked != null && product.SellerId != userId)
                {
                    await _notificationService.CreateNotificationAsync(
                        userId: product.SellerId,
                        type: "like",
                        title: "Lượt thích mới",
                        message: $"{userWhoLiked.FullName ?? userWhoLiked.Username} đã thả tym sản phẩm '{product.Title}' của bạn.",
                        referenceId: productId.ToString()
                    );
                }

                return true; // Added
            }
        }

        public async Task<List<ProductResponseDto>> GetMyWishlistProductsAsync(long userId, CancellationToken ct)
        {
            var wishlists = await _context.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Seller)
                .Include(w => w.Product)
                    .ThenInclude(p => p!.ProductImages)
                .Include(w => w.Product)
                    .ThenInclude(p => p!.Wishlists)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => w.Product)
                .ToListAsync(ct);

            return wishlists.Where(p => p != null).Select(p => new ProductResponseDto
            {
                ProductId = p!.ProductId,
                Title = p.Title,
                Price = p.Price,
                Condition = p.Condition,
                Location = p.Seller?.City ?? "Chưa cập nhật",
                ImageUrl = p.ProductImages.FirstOrDefault()?.ImageUrl,
                SellerName = p.Seller?.Username ?? "Unknown",
                IsPremium = p.HighlightStatus && p.HighlightExpiredAt > DateTime.UtcNow,
                BannerUrl = null,
                ViewCount = p.Wishlists?.Count ?? 0,
                CreatedAt = p.ProductCreateAt ?? DateTime.UtcNow
            }).ToList();
        }

        public async Task<List<long>> GetMyWishlistProductIdsAsync(long userId, CancellationToken ct)
        {
            return await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync(ct);
        }
    }
}
