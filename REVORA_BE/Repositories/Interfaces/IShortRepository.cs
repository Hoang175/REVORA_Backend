using REVORA_BE.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace REVORA_BE.Repositories.Interfaces
{
    public interface IShortRepository
    {
        Task<List<Short>> GetAllActiveShortsAsync();
        Task<bool> ChangeShortStatusAsync(long shortId, long userId, string status);
        Task<bool> ToggleLikeShortAsync(long shortId, long userId);
        Task<List<ShortComment>> GetCommentsByShortIdAsync(long shortId);
        Task<ShortComment> AddShortCommentAsync(ShortComment comment);
        Task<ShortComment?> GetCommentByIdAsync(long commentId);
        Task<ShortComment> UpdateShortCommentAsync(ShortComment comment);
        Task DeleteShortCommentAsync(long commentId);
        Task<bool> ToggleLikeCommentAsync(long commentId, long userId);
        Task<List<long>> GetLikedCommentIdsAsync(long shortId, long userId);
        Task<List<long>> GetFollowingSellerIdsAsync(long currentUserId);
    }
}