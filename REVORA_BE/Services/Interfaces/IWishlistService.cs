using REVORA_BE.DTOs.Response;

namespace REVORA_BE.Services.Interfaces
{
    public interface IWishlistService
    {
        Task<bool> ToggleWishlistAsync(long userId, long productId, CancellationToken ct);
        Task<List<ProductResponseDto>> GetMyWishlistProductsAsync(long userId, CancellationToken ct);
        Task<List<long>> GetMyWishlistProductIdsAsync(long userId, CancellationToken ct);
    }
}
