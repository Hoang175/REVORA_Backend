using REVORA_BE.DTOs.Request;
using REVORA_BE.DTOs.Response;
using REVORA_BE.Models;
using System.Threading.Tasks;

namespace REVORA_BE.Services.Interfaces
{
    public interface IProductService
    {
        Task<long> CreateProductAsync(long userId, CreateProductRequestDto request);

        Task<List<ProductResponseDto>> GetHomeProductsAsync(string type, int limit);
        Task<PagedResult<ProductResponseDto>> GetProductsWithFilterAsync(ProductFilterRequestDto filter);

        Task<PagedResult<ProductResponseDto>> GetProductsBySellerAsync(long sellerId, string status, int pageNumber, int pageSize, System.Threading.CancellationToken ct, bool isManageMode = false);
        Task<PagedResult<ProductResponseDto>> GetDeletedProductsBySellerAsync(long sellerId, int pageNumber, int pageSize, System.Threading.CancellationToken ct);
        Task<bool> UpdateProductAsync(long id, long userId, UpdateProductRequestDto request);
        Task<bool> RenewProductAsync(long id, long userId, RenewProductRequestDto request);
        Task<bool> ChangeProductStatusAsync(long id, long userId, string status);
        Task<bool> DeleteProductAsync(long id, long userId);
        Task<bool> SubmitAppealAsync(long productId, long userId, string reason);
        Task<List<UserCreditBatch>> GetActiveCreditBatchesAsync(long userId, int creditTypeId);

        Task<ProductDetailResponseDto> GetProductDetailAsync(long productId, long? currentUserId = null);

        Task<List<CommentResponseDto>> GetProductCommentsAsync(long productId, long? currentUserId);
        Task<CommentResponseDto> AddCommentAsync(long userId, long productId, CreateCommentRequestDto request);
        Task<CommentResponseDto> EditCommentAsync(long userId, long commentId, UpdateCommentRequestDto request);
        Task<bool> DeleteCommentAsync(long userId, long commentId);
        Task<bool> ToggleLikeCommentAsync(long userId, long commentId);
    }
}