using REVORA_BE.DTOs.Request;
using REVORA_BE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<List<UserCreditBatch>> GetActiveCreditBatchesAsync(long userId, int creditTypeId, bool onlyPermanent = false);
        Task CreateProductWithTransactionAsync(Product product, Short? shortVideo, List<UserCreditBatch> updatedBatches, List<CreditUsageLog>? usageLogs = null);
        Task RenewProductWithTransactionAsync(Product product, List<UserCreditBatch> updatedBatches, List<CreditUsageLog>? usageLogs = null);

        Task<Product> GetProductByIdAsync(long productId);

        Task<List<Product>> GetHomeProductsAsync(string type, int limit);
        Task<(List<Product> Items, int TotalCount)> GetProductsWithFilterAsync(ProductFilterRequestDto filter);

        Task<(List<Product> Items, int TotalCount)> GetProductsBySellerAsync(long sellerId, string status, int pageNumber, int pageSize, System.Threading.CancellationToken ct, bool isManageMode = false);
        Task<(List<Product> Items, int TotalCount)> GetDeletedProductsBySellerAsync(long sellerId, int pageNumber, int pageSize, System.Threading.CancellationToken ct);
        Task<bool> UpdateProductAsync(Product product);
        Task<bool> DeleteProductAsync(long productId);
        Task<bool> ChangeProductStatusAsync(long productId, string status);

        Task<List<ProductComment>> GetCommentsByProductIdAsync(long productId);
        Task<ProductComment?> GetCommentByIdAsync(long commentId);
        Task<User?> GetUserByIdAsync(long userId);
        Task<ProductComment> AddCommentAsync(ProductComment comment);
        Task<ProductComment> EditCommentAsync(long commentId, string newContent);
        Task<bool> DeleteCommentAsync(long commentId);
        Task<bool> ToggleLikeCommentAsync(long commentId, long userId);
        Task<bool> IsFollowingAsync(long followerId, long followeeId);
        Task<Badge?> GetBadgeByIdAsync(int badgeId);
        Task<Dictionary<int, string>> GetBadgeIdToNameMapAsync();
        Task<List<long>> GetAdminUserIdsAsync();
    }
}